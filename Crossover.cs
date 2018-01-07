using System;
using System.IO;
using System.Collections.Generic;

namespace Crossover
{
    //Compiler class
    class Crossover
    {
        static void Main(string[] args)
        {
            //Creates instances of all necessary components
            Tokenizer thisTokenizer = new Tokenizer();

            while (true)
            {
                string input = Console.ReadLine();
                
                foreach (Token token in thisTokenizer.InputToTokensList(input))
                {
                    Console.WriteLine("{0}, {1}" + Environment.NewLine, token.type.ToString(), token.value);
                }
                input = String.Empty;
            }
        }
    }
}
