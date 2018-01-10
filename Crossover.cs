using System;
using System.IO;
using System.Collections.Generic;
using Crossover;

namespace Crossover
{
    //Compiler class
    static class CrossoverCompiler
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

        public static void ThrowCompilerError(string errorText, int onLine)
        {
            string errorTextToThrow = errorText + $"(Line {onLine + 1})";

            Console.WriteLine(errorTextToThrow);
            Environment.Exit(0);
        }
    }
}
