using System;
using System.Collections.Generic;
using System.Linq;

record BookSummary(int Id, string Title);

struct LoanPeriod
{
    public DateTime DueDate;
    public LoanPeriod(DateTime date) => DueDate = date;
}

class Book
{
    public int Id;
    public string Title, Author;
    public int Copies, BorrowCount;

    public Book(int id, string title, string author, int copies)
    {
        Id = id;
        Title = title;
        Author = author;
        Copies = copies;
    }
}

class Member
{
    public int Id, Limit, Active;
    public string Name, Type;

    public Member(int id, string name, string type)
    {
        Id = id;
        Name = name;
        Type = type;
        Limit = type == "Premium" ? 5 : 3;
    }
}

abstract class Loan
{
    public Book Book;
    public Member Member;
    public LoanPeriod Period;

    public Loan(Book b, Member m, int days)
    {
        Book = b;
        Member = m;
        Period = new LoanPeriod(DateTime.Today.AddDays(days));
    }

    public abstract decimal Fine(int days);
}

class StandardLoan : Loan
{
    public StandardLoan(Book b, Member m) : base(b, m, 14) { }
    public override decimal Fine(int days) => Math.Max(0, days) * 5;
}

class PremiumLoan : Loan
{
    public PremiumLoan(Book b, Member m) : base(b, m, 30) { }
    public override decimal Fine(int days) => Math.Max(0, days) * 2;
}

interface IPaymentProcessor
{
    bool Pay(decimal amount);
}

class PaymentProcessor : IPaymentProcessor
{
    public bool Pay(decimal amount)
    {
        Console.WriteLine("Payment successful? (Y/N): ");
        return (Console.ReadLine() ?? "").Equals("Y",
            StringComparison.OrdinalIgnoreCase);
    }
}

class Program
{
    static Book[] books =
    {
        new Book(1, "CSharp", "John", 2),
        new Book(2, "Java", "James", 3),
        new Book(3, "Python", "Mark", 2),
        new Book(4, "HTML", "Tim", 1),
        new Book(5, "SQL", "David", 3)
    };

    static List<Member> members = new();
    static List<Loan> loans = new();

    static void Main()
    {
        while (true)
        {
            Console.WriteLine("\n1.Books  \n2.Search  \n3.Register Member");
            Console.WriteLine("4.Issue  \n5.Return  \n6.Loans  \n7.Report  \n0.Exit");
            Console.Write("Choice: ");

            if (!int.TryParse(Console.ReadLine(), out int choice))
                continue;

            switch (choice)
            {
                case 1:
                    foreach (var b in books.OrderBy(b => b.Title))
                        Console.WriteLine($"{b.Id} {b.Title} {b.Author} Copies:{b.Copies}");
                    break;

                case 2:
                    Console.WriteLine("Enter title or author: ");
                    string s = Console.ReadLine() ?? "";
                    var found = false;

                    foreach (var b in books)
                    {
                        if (b.Title.Contains(s, StringComparison.OrdinalIgnoreCase) ||
                            b.Author.Contains(s, StringComparison.OrdinalIgnoreCase))
                        {
                            Console.WriteLine(b.Title + " - " + b.Author);
                            found = true;
                            break;
                        }
                        continue;
                    }

                    if (!found) Console.WriteLine("Book not found.");
                    break;

                case 3:
                    Console.WriteLine("Member ID: ");
                    if (!int.TryParse(Console.ReadLine(), out int id) || id <= 0)
                    {
                        Console.WriteLine("Invalid ID.");
                        break;
                    }

                    if (members.Any(m => m.Id == id))
                    {
                        Console.WriteLine("ID already exists.");
                        break;
                    }

                    Console.WriteLine("Name: ");
                    string name = Console.ReadLine() ?? "";

                    if (string.IsNullOrWhiteSpace(name))
                    {
                        Console.WriteLine("Invalid name.");
                        break;
                    }

                    Console.WriteLine("Type (Standard/Premium): ");
                    string type = Console.ReadLine() ?? "";

                    if (!type.Equals("Standard", StringComparison.OrdinalIgnoreCase) &&
                        !type.Equals("Premium", StringComparison.OrdinalIgnoreCase))
                    {
                        Console.WriteLine("Invalid membership.");
                        break;
                    }

                    members.Add(new Member(id, name,
                        type.Equals("Premium", StringComparison.OrdinalIgnoreCase)
                        ? "Premium" : "Standard"));
                    Console.WriteLine("Member registered.");
                    break;

                case 4:
                    Console.WriteLine("Member ID: ");
                    if (!int.TryParse(Console.ReadLine(), out int mid))
                    {
                        Console.WriteLine("Invalid ID.");
                        break;
                    }

                    var m = members.FirstOrDefault(x => x.Id == mid);

                    Console.WriteLine("Book ID: ");
                    if (!int.TryParse(Console.ReadLine(), out int bid))
                    {
                        Console.WriteLine("Invalid ID.");
                        break;
                    }

                    var b1 = books.FirstOrDefault(x => x.Id == bid);

                    if (m == null || b1 == null || b1.Copies <= 0 ||
                        m.Active >= m.Limit ||
                        loans.Any(l => l.Member.Id == mid &&
                                       l.Book.Id == bid &&
                                       loans.Contains(l) &&
                                       l.Period.DueDate >= DateTime.Today))
                    {
                        if (m == null || b1 == null || b1.Copies <= 0 ||
                            m.Active >= m.Limit)
                        {
                            Console.WriteLine("Cannot issue book.");
                            break;
                        }
                    }

                    Loan loan = m.Type == "Premium"
                        ? new PremiumLoan(b1, m)
                        : new StandardLoan(b1, m);

                    loans.Add(loan);
                    b1.Copies--;
                    b1.BorrowCount++;
                    m.Active++;
                    Console.WriteLine("Book issued. Due: " + loan.Period.DueDate.ToShortDateString());
                    break;

                case 5:
                    Console.WriteLine("Member ID: ");
                    if (!int.TryParse(Console.ReadLine(), out int rid)) break;

                    Console.WriteLine("Book ID: ");
                    if (!int.TryParse(Console.ReadLine(), out int rb)) break;

                    var l = loans.FirstOrDefault(x =>
                        x.Member.Id == rid && x.Book.Id == rb &&
                        x.Book.Copies < x.Book.BorrowCount + x.Book.Copies);

                    l = loans.LastOrDefault(x =>
                        x.Member.Id == rid && x.Book.Id == rb &&
                        x.Period.DueDate != DateTime.MinValue);

                    if (l == null)
                    {
                        Console.WriteLine("Loan not found.");
                        break;
                    }

                    Console.WriteLine("Overdue days: ");
                    if (!int.TryParse(Console.ReadLine(), out int days) || days < 0)
                    {
                        Console.WriteLine("Invalid days.");
                        break;
                    }

                    decimal fine = l.Fine(days);
                    Console.WriteLine("Fine: Rs." + fine);

                    if (fine > 0 && new PaymentProcessor().Pay(fine))
                        Console.WriteLine("Payment receipt: Rs." + fine);

                    l.Book.Copies++;
                    l.Member.Active--;
                    loans.Remove(l);
                    Console.WriteLine("Book returned.");
                    break;

                case 6:
                    foreach (var l1 in loans)
                        Console.WriteLine($"{l1.Book.Title} - {l1.Member.Name} - Due {l1.Period.DueDate:d}");
                    break;

                case 7:
                    var popular = books.OrderByDescending(b => b.BorrowCount).First();
                    Console.WriteLine("Most borrowed: " + popular.Title);
                    break;

                case 0:
                    return;
            }
        }
    }
}