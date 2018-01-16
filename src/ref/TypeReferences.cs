using System;
using System.Linq;
using System.Collections.Generic;
using Crossover;

namespace Crossover
{
    class Variable
    {
        public bool isExclusive = false;
        public int depthLevel;
        public string name;

        public string value;
        public TokenType type;
    }

    class Function
    {
        public AccessLevel scope = AccessLevel.Public;
        public string name;
        public List<Parameter> parameters = new List<Parameter>();

        public List<Token> contents = new List<Token>();
    }

    class Parameter
    {
        public List<Variable> parameterVariables = new List<Variable>();

        
    }

    enum AccessLevel
    {
        Public,
        Exclusive   //exclusive keyword
    }
}