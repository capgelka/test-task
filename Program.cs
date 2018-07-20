using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;

using System.Threading.Tasks;

using Microsoft.SolverFoundation.Solvers;


static class Constants
{
    public const bool Debug = false;
}

namespace test_task
{
    public enum TokenType
    {
        LBracket,
        RBracket,
        LSquareBracket,
        RSquareBracket,
        LParentheses,
        RParentheses,
        Semicolon,
        Equals,
        Other,
        IntType,
        If,
        Sink,
        Number,
        Space,
        Identifier,
        ThreeDots,
    }

    public enum NodeType
    {
       Block,
       If,
       Declaration,
       Assigment,
       Lookup,
       VarName,
       Number,
       Sink,
    }

    public class TreeNode
    {
        public TreeNode(string value, NodeType type) {
            Type = type;
            Value = value;
        }
        
        public NodeType Type { get; }
        public string Value { get; }

    }

    public abstract class IVisitor<T>
    {
        // no need to visit node twice, thus no problem with this debug thing enabled
        private HashSet<TreeNode> Visited { get; set; }

        public T Visit(AST ast)
        {
            if (Constants.Debug) {
                Console.WriteLine("Call on {0}, depth {1}", ast.Node.Type, ast.Depth());
                ast.Show();
                if (Visited == null) {
                    Visited = new HashSet<TreeNode>();
                }
                if (ast.Node != null && Visited.Contains(ast.Node)) {
                    Console.WriteLine(
                        String.Format(
                            "{0} ({1}) has been already visited",
                            ast.Node.Type,
                            ast.Node.Value
                        )
                    );
                    Environment.Exit(5);
                }
                Visited.Add(ast.Node);
            }
            switch(ast.Node.Type)
            {
                case NodeType.Block:
                    return this.BlockVisitor(ast);
                case NodeType.If:
                    return this.IfVisitor(ast);
                case NodeType.Declaration:
                    return this.DeclarationVisitor(ast);
                case NodeType.Assigment:
                    return this.AssigmentVisitor(ast);
                case NodeType.Lookup:
                    return this.LookupVisitor(ast);
                case NodeType.VarName:
                    return this.VarNameVisitor(ast);
                case NodeType.Number:
                    return this.NumberVisitor(ast);
                case NodeType.Sink:
                    return this.SinkVisitor(ast);
                default:
                    Console.WriteLine("WTF?");
                    Environment.Exit(10);
                    return this.BlockVisitor(ast);
            }
        }

        public abstract T BlockVisitor(AST ast);

        public abstract T IfVisitor(AST ast);

        public abstract T DeclarationVisitor(AST ast);

        public abstract T AssigmentVisitor(AST ast);

        public abstract T LookupVisitor(AST ast);

        public abstract T VarNameVisitor(AST ast);

        public abstract T NumberVisitor(AST ast);

        public abstract T SinkVisitor(AST ast);
    }


    public class SExprVisitor : IVisitor<String>
    {

        public override String BlockVisitor(AST ast)
        {
            return String.Format(
                "(\n\t{0})",
                string.Join("\n", ast.Children.Select(a => this.Visit(a)))
            );
        }

        public override String IfVisitor(AST ast)
        {
            return String.Format(
                "(if ({0}) {1})",
                this.Visit(ast.Children[0]),
                string.Join("\n", ast.Children.Skip(1).Select(a => this.Visit(a)))
            );
        }

        public override String DeclarationVisitor(AST ast)
        {
            return String.Format(
                "(set {0})",
                string.Join(" ", ast.Children.Select(a => this.Visit(a)))
            );
        }

        public override String AssigmentVisitor(AST ast)
        {
            return String.Format(
                "(= {0})",
                string.Join(" ", ast.Children.Select(a => this.Visit(a)))
            );
        }

        public override String LookupVisitor(AST ast)
        {
            return String.Format(
                "{0}[{1}]",
                this.Visit(ast.Children[0]),
                this.Visit(ast.Children[1])
            );
        }

        public override String VarNameVisitor(AST ast)
        {
            return ast.Node.Value;
        }

        public override String NumberVisitor(AST ast)
        {
            return ast.Node.Value;
        }

        public override String SinkVisitor(AST ast)
        {
            return String.Format("(sink {0})", this.Visit(ast.Children.Last()));
        }

    }

    // public class Solver
    // {
    //     public Solver()
    //     {
    //         Commands = "";
    //     }

    //     public AddConstraints(String )

    //     public static Solver CreateSolver()
    //     {
    //         return new Solver();
    //     }

    // }


    public class State
    {
        public State()
        {
            Info = "";
            Vars = new Dictionary<String, CspTerm>();
            Data = new Dictionary<int, HashSet<CspTerm>>();
            PathConstraints = new HashSet<CspTerm>();

            Cs = ConstraintSystem.CreateSolver();
        }  

        private Dictionary<int, HashSet<CspTerm>> Data { get; set; }
        private Dictionary<String, CspTerm> Vars { get; set; }

        public String Info { get; set; }
        private ConstraintSystem Cs { get; }
        private HashSet<CspTerm> PathConstraints { get; }

        public void AddVar(int value)
        {
            Data.Add(value, new HashSet<CspTerm> {Cs.True});
            foreach (var cons in PathConstraints) {
                Data[value].Add(cons);
            }
        }

        public State Update(State other)
        {
            // foreach (var k in other.Vars.Keys){
            //     Vars.Add(k, other.Vars[k]);
            // }
            // foreach(var k in other.Data.Keys) {
            //     if (!Data.ContainsKey(k)) {
            //         this.AddVar(k);
            //         foreach(var c in other.Data[k]) {
            //             Data[k].Add(c);
            //         }
            //     }
            //     else {
            //         // CspTerm tmp = Cs.True;
            //         // foreach(var c in other.Data[k]) {
            //         //     tmp = Cs.And(tmp, c);
            //         // }
            //         ;
            //         Data[k].Add(
            //             other.Data[k].Aggregate(
            //                 Cs.True,
            //                 (acc, el) => Cs.And(acc, el)
            //             )
            //         );

            //     }
            // }
            return this;
        }

        public State UpdateOnConstraint(String cName)
        {
            if (!Vars.ContainsKey(cName)) {
                Vars.Add(cName, Cs.CreateBoolean(cName));
            }

            foreach (var k in Data.Keys) {
                Data[k].Add(Vars[cName]);
            }
            PathConstraints.Add(Vars[cName]);

            // Console.WriteLine("==");
            // foreach (var val in Vars.Keys) {
            //     Console.WriteLine(val);
            //  }

            return this;
        }

        public List<int> PossibleValues()
        {
            var buff = new List<int>();
            foreach (var k in Data.Keys) {
                // foreach (var x in Data[k].Variables) {
                //    Console.WriteLine("++++");
                //    Console.WriteLine(x);
                // }
                // foreach (var x in Data[k].Constraints) {
                //    Console.WriteLine("-----");
                //    Console.WriteLine(x);
                // }
                Console.WriteLine("Solving for {0}", k);
                foreach (var cons in Data[k]) {
                    Console.WriteLine(Cs.AddConstraints(cons));
                    Console.WriteLine(cons);
                }

                foreach (var x in Cs.Constraints) {
                    Console.WriteLine(x);
                }

                // var task = Task<ConstraintSolverSolution>.Factory.StartNew(() => Cs.Solve());
                // task.Wait();
                // var solution = task.Result;
                var solution = Cs.Solve();
                if (solution.HasFoundSolution) {

                    // foreach (var val in Vars.Keys) {
                    //     Console.WriteLine("=_=");
                    //     //Console.WriteLine(solution[key]);
                    // }

                    foreach (var condKey in Vars.Keys) {
                        Console.WriteLine("!!!");
                        Console.WriteLine("condition[{0}]: {1}", condKey, Vars[condKey]);
                        try {
                            Console.WriteLine(solution[Vars[condKey]]);
                        }

                        catch {
                            Console.WriteLine("(");
                        }
                    }

                    buff.Add(k);
                }
                Cs.RemoveConstraints();
            }
            return buff;
        }

    }

    public class SAVisitor : IVisitor<State>
    {
        // SAVisitor()
        // {
        //     Conditions = new HashSet<String>();
        // }

        // private HashSet<String> Conditions { get; set; }

        public override State BlockVisitor(AST ast)
        {
            State st = new State();
            foreach(State s in ast.Children.Select(c => this.Visit(c))) {
                if (s == null) {
                    ;
                }
                else if (s.Info == "") {
                    st.Update(s);
                }
                else {
                    Console.WriteLine("Add {0}", s.Info);
                    st.AddVar(Convert.ToInt32(s.Info));
                }
            }
            return st;
        }

        public override State IfVisitor(AST ast)
        {
            return Visit(ast.Children[1]).UpdateOnConstraint(
                Visit(ast.Children[0]).Info
            );
            // return new State().UpdateOnConstraint(
            //     ,
            //     Visit(ast.Children[1])
            // );
        }

        // don't need this
        public override State DeclarationVisitor(AST ast)
        {
            return null;
        }

        public override State AssigmentVisitor(AST ast)
        {
            State st = Visit(ast.Children[1]);
            // st.Node = new TreeNode("", NodeType.Assigment);
            return st;
        }

        public override State LookupVisitor(AST ast)
        {
            var st = Visit(ast.Children[1]);

            // Conditions.Add(st.Info);
            return st;
        }

        public override State VarNameVisitor(AST ast)
        {
            State st = new State();
            st.Info = ast.Node.Value;
            return st;
        }

        public override State NumberVisitor(AST ast)
        {
            State st = new State();
            st.Info = ast.Node.Value;
            return st;
        }

        public override State SinkVisitor(AST ast)
        {
            return new State();
        }

    }

    public class Token
    {
        public Token(String value, TokenType token) {
            Type = token;
            Value = value;
        }

        public TokenType Type { get; }
        public string Value { get; }

        public bool isSpace() {
            return this.Type == TokenType.Space;
        }

        public bool isIdentifier() {
            return this.Type == TokenType.Identifier;
        }

        public bool isConcatable() {
            return (
                this.isSpace() || 
                this.isIdentifier() && 
                !(new Regex(@"\d+$").IsMatch(this.Value))
            );
        }

        public static Token Match(string value) {
            switch(value)
            {
                case "{":
                    return new Token(value, TokenType.LBracket);
                case "}":
                    return new Token(value, TokenType.RBracket);
                case "[":
                    return new Token(value, TokenType.LSquareBracket);
                case "]":
                    return new Token(value, TokenType.RSquareBracket);
                case "(":
                    return new Token(value, TokenType.LParentheses);
                case ")":
                    return new Token(value, TokenType.RParentheses);
                case ";":
                    return new Token(value, TokenType.Semicolon);
                case "=":
                    return new Token(value, TokenType.Equals);
                case "if":
                    return new Token(value, TokenType.If);
                case "int":
                    return new Token(value, TokenType.IntType);
                case "...":
                    return new Token(value, TokenType.ThreeDots);
                case "System.out.println":
                    return new Token(value, TokenType.Sink);
                case var sv when new Regex(@"^[\s\r\n]+$").IsMatch(sv):
                    return new Token(value, TokenType.Space);
                case var sv when new Regex(@"^\d+$").IsMatch(sv):
                    return new Token(value, TokenType.Number);
                case var sv when new Regex(@"^[\w+_][\w_\.\d]*[\d\w_]*$").IsMatch(sv):
                    return new Token(value, TokenType.Identifier);
                default:
                    return null;
            }
        }

    }

    public class Lexer
    {
        public static List<Token> Tokenize(string source)
        {
            List<Token> tokens = new List<Token>();
            List<char> buff = new List<char>();
            Token tok;
            List<char> old_buff = null;
            foreach (char letter in source) {
                buff.Add(letter);
                tok = Lexer.Match(buff);
                if (tok == null && old_buff != null) {
                    tokens.Add(Lexer.Match(old_buff));
                    old_buff = null;
                    buff = new List<char>();
                    buff.Add(letter);
                    tok = Lexer.Match(buff);
                }
                if (tok != null && !tok.isConcatable()) {
                    tokens.Add(tok);
                    buff = new List<char>();
                    old_buff = null;
                }
                else {
                    old_buff = new List<char>(buff);
                }
            }
            return tokens;
        }

        private static Token Match(List<char> buffer) {
            return Token.Match(
                string.Join("", buffer.ToArray())
            );
        }
    }

    public class Parser
    {
        public Parser(IEnumerable<Token> input) {
            Source = input;
            Ast = new AST();
            this.Prepare();
        }


        public AST Ast { get; }
        private IEnumerable<Token> Source { get; set; }

        // due restrictions no need to handle some things
        private void Prepare() {
            var uselessTokens = new List<TokenType> {
                TokenType.LParentheses,
                TokenType.RParentheses,
                TokenType.Semicolon,
                TokenType.Space,
                TokenType.Equals,
                TokenType.LSquareBracket,
                TokenType.RSquareBracket,
            };
            this.Source = (
                this.Source
                    .SkipWhile(t => t.Type != TokenType.LBracket)
                    .SkipWhile(t => t.Type == TokenType.LBracket)
                    .SkipWhile(t => t.Type != TokenType.LBracket)
                    .Where(t => !uselessTokens.Contains(t.Type))
            );
            if (Constants.Debug) {
                foreach (Token tok in Source) {
                    Console.WriteLine(
                        String.Format("{0} {1}", tok.Type, tok.Value)
                    );
                }
            }
        }

        private void Error(string Message) {
            Console.WriteLine("Parse error: {0}", Message);
            Environment.Exit(3);
        }

        private Token Next(IEnumerator<Token> stream) {
            stream.MoveNext();
            return stream.Current;
        }

        public AST Parse() {
            return ParseStart(this.Source);
        }

        private AST ParseStart(IEnumerable<Token> stream) {
            AST current = this.Ast;
            var tokens = stream.GetEnumerator();
            if (Next(tokens).Type == TokenType.LBracket) {
                return this.ParseBlock(current, tokens);
            }
            this.Error("No entry block found");
            return null; // Stupid compiler
        }

        private AST ParseBlock(AST current, IEnumerator<Token> stream) {
            AST ast;
            if (current.isRoot()) {
                current.Node = new TreeNode("RootBlock", NodeType.Block);
                ast = current;
            }
            else {
                ast = new AST(
                    new TreeNode("Block", NodeType.Block),
                    current
                );
            }
            Token tok;
            while (stream.MoveNext()) {
                tok = stream.Current;
            // foreach(Token tok in stream) {
                switch (tok.Type)
                {   
                    case TokenType.IntType:
                        ast.Children.Add(
                            ParseDeclaration(ast, stream)
                        );
                        break;
                    case TokenType.Identifier:
                        ast.Children.Add(
                            ParseAssigment(ast, tok, stream)
                        );
                        break;
                    case TokenType.If:
                        ast.Children.Add(
                            ParseIf(ast, stream)
                        );
                        break;
                    case TokenType.Sink:
                        ast.Children.Add(
                            ParseSink(ast, stream)
                        );
                        break;
                    case TokenType.RBracket:
                        return ast;
                    default:
                        this.Error(
                            String.Format("{0} is not valid Token for block", tok.Type)
                        );
                        break;
                }
            }
            this.Error("Unexpected stream end");
            return ast;
        }


        private AST ParseSink(AST current, IEnumerator<Token> stream) {
            AST ast = new AST(
                new TreeNode("System.out.println", NodeType.Sink),
                current
            );
            Token token = Next(stream);
            if (token.Type != TokenType.Identifier) {
                this.Error("There must be varname in sink end");
            }
            ast.Children.Add(
                new AST(
                    new TreeNode(token.Value, NodeType.VarName),
                    ast
                )
            );
            return ast;
        }


        private AST ParseIf(AST current, IEnumerator<Token> stream) {
            Token tok = Next(stream);
            if (tok.Type != TokenType.Identifier) {
                this.Error("There must be an identifier in the start of if condition");
            }
            AST ast = new AST(
                new TreeNode("", NodeType.If),
                current
            );
            Token nextToken = Next(stream);
            switch(nextToken.Type)
            {
                case TokenType.Number:
                    ast.Children.Add(
                        ParseLookup(ast, tok, nextToken)
                    );
                    if (Next(stream).Type != TokenType.LBracket) {
                        this.Error("If must have a body block after condition");
                    }
                    break;
                case TokenType.LBracket:
                    ast.Children.Add(
                        new AST(
                            new TreeNode(tok.Value, NodeType.Number),
                            ast
                        )
                    );
                    break;
                default:
                    this.Error("Unexpected token in If block");
                    break;
            }
            ast.Children.Add(
                ParseBlock(ast, stream)
            );
            return ast;
        }

        private AST ParseLookup(AST current, Token varname, Token index) {
            AST ast = new AST(
                new TreeNode("", NodeType.Lookup),
                current
            );
            ast.Children.Add(
                new AST(
                    new TreeNode(varname.Value, NodeType.VarName),
                    ast
                )
            );
            ast.Children.Add(
                new AST(
                    new TreeNode(index.Value, NodeType.Number),
                    ast
                )
            );
            return ast;
        }

        private AST ParseDeclaration(AST current, IEnumerator<Token> stream) {
            Token token = Next(stream);

            if (token.Type != TokenType.Identifier) {
                this.Error(
                    String.Format(
                        "There must be an identifier after type declaration, not {0}",
                        token.Type
                    )
                );
            }

            AST ast = new AST(
                new TreeNode("", NodeType.Declaration),
                current
            );
            ast.Children.Add(
                new AST(
                    new TreeNode(token.Value, NodeType.VarName),
                    ast
                )
            );
            return ast;
        }

        private AST ParseAssigment(AST current, Token token, IEnumerator<Token> stream) {
            Token nextToken = Next(stream);

            if (token.Type != TokenType.Identifier) {
                this.Error("There must be a number in the end of assigment expression");
            }
            AST ast = new AST(
                new TreeNode("", NodeType.Assigment),
                current
            );
            ast.Children.Add(
                new AST(
                    new TreeNode(token.Value, NodeType.VarName),
                    ast
                )
            );
            ast.Children.Add(
                new AST(
                    new TreeNode(nextToken.Value, NodeType.Number),
                    ast
                )
            );
            return ast;
        }
    }


    public class AST
    {
        public AST() {
            Node = null;
            Parent = null;
            Children = new List <AST>();
        }

        public AST(TreeNode node, AST parent) {
            Node = node;
            Parent = parent;
            Children = new List <AST>();
        }

        public AST(AST parent) {
            Node = null;
            Parent = parent;
            Children = new List <AST>();
        }


        public TreeNode Node { get; set; }
        public AST Parent { get; }
        public List<AST> Children { get; }


        public bool isLeaf() {
            return this.Children == null;
        }

        public bool isRoot() {
            return this.Parent == null;
        }

        public int Depth() {
            int depth = 0;
            AST current = this;
            while (!current.isRoot()) {
                depth++;
                current = current.Parent;
            }
            return depth;
        }

        public void Show() => Console.WriteLine(
                Node.Type +
                ": <" +
                string.Join(", ", Children.Select(
                    s => (s.Node.Type + " (\"" + s.Node.Value + "\")"))
                ) +
                ">"
            );


    }


    static class Programm
    {
        public static int Main(string[] args)
        {

            //StreamReader sr;
            string source;
            if (args.Length == 0)
            {
                System.Console.WriteLine("You need to specify source file");
                return 1;
            }
            try
            {
                source = System.IO.File.ReadAllText(args[0]);
            }
            catch (Exception e)
            {
                Console.WriteLine("The file could not be read:");
                Console.WriteLine(e.Message);
                return 2;
            }

            AST ast = new Parser(
                Lexer.Tokenize(source)
            ).Parse();
            if (Constants.Debug) {
                Console.WriteLine(new SExprVisitor().Visit(ast));
            }
            var result = new SAVisitor().Visit(ast);
            Console.WriteLine(
                String.Format(
                    "[{0}]",
                    String.Join(
                        ", ",
                        result.PossibleValues()
                    )
                )
            );


            // ConstraintSystem s1 = ConstraintSystem.CreateSolver();

            // CspTerm t1 = s1.CreateBoolean("v1");
            // CspTerm t2 = s1.CreateBoolean("v2");
            // CspTerm t3 = s1.CreateBoolean("v3");
            // CspTerm t4 = s1.CreateBoolean("v4");

            // CspTerm tOr12 = s1.Or(s1.Not(t1), s1.Not(t2));
            // CspTerm tOr13 = s1.Or(s1.Not(t1), s1.Not(t3));
            // CspTerm tOr14 = s1.Or(s1.Not(t1), s1.Not(t4));

            // CspTerm tOr23 = s1.Or(s1.Not(t2), s1.Not(t3));
            // CspTerm tOr24 = s1.Or(s1.Not(t2), s1.Not(t4));

            // CspTerm tOr34 = s1.Or(s1.Not(t3), s1.Not(t4));

            // CspTerm tOr = s1.Or(t1, t2, t3, t4);

            // // s1.AddConstraints(tOr12);
            // // s1.AddConstraints(tOr13);
            // // s1.AddConstraints(tOr14);
            // // s1.AddConstraints(tOr23);
            // // s1.AddConstraints(tOr24);
            // // s1.AddConstraints(tOr34);
            // // s1.AddConstraints(tOr);
            // s1.AddConstraints(s1.True);

            // foreach (var x in s1.Constraints) {
            //     Console.WriteLine(x);
            // }
            // Console.WriteLine(t1);

            // ConstraintSolverSolution solution1 = s1.Solve();
            // Console.WriteLine(solution1[t1]);
            // Console.WriteLine(solution1[t2]);
            // Console.WriteLine(solution1[t3]);
            // Console.WriteLine(solution1[t4]);



            // // s2.AddConstraints(tOr12);
            // // s2.AddConstraints(tOr13);
            // // s2.AddConstraints(tOr14);
            // // s2.AddConstraints(tOr23);
            // // s2.AddConstraints(tOr24);
            // // s2.AddConstraints(tOr34);
            // // s2.AddConstraints(tOr);

            // // ConstraintSolverSolution sol2 = s2.Solve();
            // // Console.WriteLine(sol2[t1]);
            // // Console.WriteLine(sol2[t2]);
            // // Console.WriteLine(sol2[t3]);
            // // Console.WriteLine(sol2[t4]);


            return 0;
        }
    }
}