using System;

namespace GoogleKeep.Test
{
    class Program
    {
        static void Main(string[] _)
        {
            IKeep keep = new Keep();

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
