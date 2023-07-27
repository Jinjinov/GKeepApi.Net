using System;

namespace GKeepApi.Net.Test
{
    class Program
    {
        static void Main(string[] _)
        {
            Console.WriteLine("Google account email: ");
            string email = Console.ReadLine();

            Console.WriteLine("Password: ");
            string password = Console.ReadLine();

            IKeep keep = new Keep();

            keep.Login(email, password);

            var notes = keep.All();

            foreach (var note in notes)
            {
                Console.WriteLine("Title:");
                Console.WriteLine(note.Title);
                Console.WriteLine("Text:");
                Console.WriteLine(note.Text);
                Console.WriteLine();
            }

            Console.WriteLine("Press Enter to exit");
            Console.ReadLine();
        }
    }
}
