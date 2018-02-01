using System;
using System.Linq;
using System.Collections.Generic;
using Crossover;

namespace Crossover
{
    class Variable
    {
        //Parent script of this variable
        public string parentScript;

        //Exclusive or not?
        public bool isExclusive = false;

        //Depth level that this variable was instantiated at
        public int depthLevel;

        //Name of this variable
        public string name;

        //Value of this variable
        public string value;

        //Type of this variable
        public TokenType type;
    }

    class Function
    {
        //Parent script of this function
        public string parentScript;

        //Access level (scope) of this function
        public AccessLevel scope = AccessLevel.Public;

        //Name of this function
        public string name;

        //Parameters that are taken in by this function
        public List<Parameter> parameters = new List<Parameter>();

        //Contents (in token form) of this function
        public List<Token> contents = new List<Token>();
    }

    class Parameter
    {
        public Variable parameterVariable;

        Parameter(Variable paramVariable)
        {
            parameterVariable = paramVariable;
        }
    }

    //Use when passing parameters into a function
    class PassedParameter
    {
        public List<Token> contents = new List<Token>();
    }

    enum AccessLevel
    {
        Public,
        Exclusive   //exclusive keyword
    }
}