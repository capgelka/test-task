using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;

using Decider.Csp.BaseTypes;
using Decider.Csp.Integer;


namespace test_task
{   

    public class Config
    {
        public static bool Debug = false;
    }


    public class SATSolver
    {

        public SATSolver()
        {
            Constraints = new HashSet<ExpressionInteger>();
            Vars = new Dictionary<String, VariableInteger>();
            AddVariable("__system"); // avoid failing on empty solver calls
        }

        public HashSet<ExpressionInteger> Constraints { get; private set;}
        public Dictionary<String, VariableInteger> Vars { get; }

        public VariableInteger AddVariable(String name)
        {
            if (Vars.ContainsKey(name)) {
                return Vars[name];
            }
            var vi = new VariableInteger(name, 0, 1);
            Vars.Add(name, vi);
            return vi;
        }

        public VariableInteger CreateVariable(String name)
        {
            return new VariableInteger(name, 0, 1);
        }


        public ExpressionInteger And(ExpressionInteger a, ExpressionInteger b)
        {
            return a & b;
        }

        public ExpressionInteger Or(ExpressionInteger a, ExpressionInteger b)
        {
            return a | b;
        }

        public ExpressionInteger Not(ExpressionInteger a)
        {
            return !a;
        }

        public void AddConstraint(ExpressionInteger constr)
        {   
            Constraints.Add(constr);
        }

        public bool Solve()
        {
            return _Solve(Vars.Values, Constraints);
        }

        public bool Solve(IEnumerable<ExpressionInteger> cl)
        {
            return _Solve(Vars.Values, cl);
        }

        public bool Solve(IEnumerable<VariableInteger> vars, IEnumerable<ExpressionInteger> cl)
        {
            return _Solve(vars, cl);
        }

        private bool _Solve(IEnumerable<VariableInteger> vars, IEnumerable<ExpressionInteger> cl)
        {
            var constraints = cl.Select(e => new ConstraintInteger(e == 1));

            StateOperationResult searchResult;

            IState<int> state = new StateInteger(vars, constraints);
            state.StartSearch(out searchResult);
            if (Config.Debug) {
                if (searchResult == StateOperationResult.Solved) {
                    Console.WriteLine("**Solution***");
                    foreach (var v in vars) {
                        Console.WriteLine("{0} {1}", v.Name, v);
                    }
                }
            }
            return (searchResult == StateOperationResult.Solved);

        }

        public void RemoveConstraints()
        {
            Constraints = new HashSet<ExpressionInteger>();
        }

    }


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
        private HashSet<TreeNode> Visited { get; set; }

        public T Visit(AST ast)
        {
            if (Config.Debug) {
                Console.WriteLine("Call on {0}, depth {1}", ast.Node.Type, ast.Depth());
                ast.Show();
                if (Visited == null) {
                    Visited = new HashSet<TreeNode>();
                }
                if (ast.Node != null && Visited.Contains(ast.Node)) {
                    Console.WriteLine(
                        String.Format(
                            "{0} ({1}) has been already visited!",
                            ast.Node.Type,
                            ast.Node.Value
                        )
                    );
                }
                Visited.Add(ast.Node);
            }
            switch(ast.Node.Type)
            {
                case NodeType.Block:
                    return BlockVisitor(ast);
                case NodeType.If:
                    return IfVisitor(ast);
                case NodeType.Declaration:
                    return DeclarationVisitor(ast);
                case NodeType.Assigment:
                    return AssigmentVisitor(ast);
                case NodeType.Lookup:
                    return LookupVisitor(ast);
                case NodeType.VarName:
                    return VarNameVisitor(ast);
                case NodeType.Number:
                    return NumberVisitor(ast);
                case NodeType.Sink:
                    return SinkVisitor(ast);
                default:
                    Console.WriteLine("WTF?");
                    Environment.Exit(10);
                    return BlockVisitor(ast);
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
                string.Join("\n", ast.Children.Select(Visit))
            );
        }

        public override String IfVisitor(AST ast)
        {
            return String.Format(
                "(if ({0}) {1})",
                Visit(ast.Children[0]),
                string.Join("\n", ast.Children.Skip(1).Select(Visit))
            );
        }

        public override String DeclarationVisitor(AST ast)
        {
            return String.Format(
                "(set {0})",
                string.Join(" ", ast.Children.Select(Visit))
            );
        }

        public override String AssigmentVisitor(AST ast)
        {
            return String.Format(
                "(= {0})",
                string.Join(" ", ast.Children.Select(Visit))
            );
        }

        public override String LookupVisitor(AST ast)
        {
            return String.Format(
                "{0}[{1}]",
                Visit(ast.Children[0]),
                Visit(ast.Children[1])
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
            return String.Format("(sink {0})", Visit(ast.Children.Last()));
        }

    }

    // it's much better to create a new tree but nothing bad 
    // to modify the original in this case, which is much easier
    public class SimplificationVisior : IVisitor<AST>
    {

        public override AST BlockVisitor(AST ast)
        {
            var children = new List<AST>();
            bool assigmentExist = false;
            ast.Children.Reverse();
            foreach (var child in ast.Children) {
                if (assigmentExist) {
                    break;
                }

                if (child.Node.Type == NodeType.Assigment) {
                    assigmentExist = true;
                }
                children.Add(Visit(child));
            }
            children.Reverse();
            ast.Children = children;
            return ast;
        }

        public override AST IfVisitor(AST ast)
        {
            foreach (var v in ast.Children) {
                Visit(v);
            }
            return ast;
        }

        public override AST DeclarationVisitor(AST ast)
        {
            return ast;
        }

        public override AST AssigmentVisitor(AST ast)
        {
            return ast;
        }

        public override AST LookupVisitor(AST ast)
        {
            return ast;
        }

        public override AST VarNameVisitor(AST ast)
        {
            return ast;
        }

        public override AST NumberVisitor(AST ast)
        {
            return ast;
        }

        public override AST SinkVisitor(AST ast)
        {
            return ast;
        }

    }

    public class SAVisitor : IVisitor<String>
    {
        public SAVisitor()
        {
            PathConstraints = new Dictionary<AST, HashSet<String>>();
            Data = new Dictionary<int, HashSet<AST>>();
        }   

        private Dictionary<AST, HashSet<String>> PathConstraints {get; set; }
        private Dictionary<int, HashSet<AST>> Data {get; set; }

        private String VarName(String name)
        {
            if (name.StartsWith("!")) {
                return name.Substring(1);
            }
            return name;
        }

        private SATSolver PrepareSolver(int value)
        {
            var Solver = new SATSolver();
            List<ExpressionInteger> buff = new List<ExpressionInteger>();
            foreach (var key in Data[value]) {
                var tmp = new List<ExpressionInteger>();
                if (Config.Debug) {
                    Console.WriteLine("working on key {0}", value);
                }
                foreach (var c in PathConstraints[key]) {
                    if (Config.Debug) {
                        Console.WriteLine("\tworking on var {0}", c);
                    }
                    var expr = Solver.AddVariable(VarName(c));
                    if (c.StartsWith("!")) {
                        tmp.Add(
                            Solver.Not(expr)
                        );
                    } else {
                        tmp.Add(expr);
                    }
                }
                if (tmp.Count() > 0) {
                    buff.Add(
                        tmp.Aggregate(
                            (acc, el) => Solver.And(acc, el)
                        )
                    );
                }
            }
            if (buff.Count() > 0) {
                Solver.AddConstraint(
                    buff.Aggregate(
                        (acc, el) => Solver.Or(acc, el)
                    )
                );
            }
            return Solver;
        }

        public List<int> PossibleValues()
        {
            var buff = new List<int>();
            if (Config.Debug) {
                Console.WriteLine("-------Solving constraints-------");
            }
            foreach (var k in Data.Keys) {
                var Solver = PrepareSolver(k);
                if (Config.Debug) {
                    Console.WriteLine("Solving for {0}", k);
                }

                if (Solver.Solve()) {
                    buff.Add(k);
                }
            }
            return buff;
        }


        private String Not(String name) 
        {
            if (name.StartsWith("!")) {
                return name;
            }
            return "!" + name;
        }

        public override String BlockVisitor(AST ast)
        {   
            if (!PathConstraints.ContainsKey(ast)) {
                PathConstraints.Add(ast, new HashSet<String>());
            }
            if (!ast.isRoot()) {
                PathConstraints[ast].UnionWith(PathConstraints[ast.Parent]);
            }
            var buff = new HashSet<AST>();
            foreach(var c in ast.Children) {
                if (c.Node.Type == NodeType.If) {
                    var constr = Visit(c.Children[0]);
                    if (!PathConstraints.ContainsKey(c)) {
                        PathConstraints.Add(c, new HashSet<String>());
                    }
                    if (buff.Count() > 0) {
                        foreach(var b in buff) {
                            if (Config.Debug) {
                                Console.WriteLine("NEgative const on {0} for ", constr);
                                b.Show();
                            }
                            PathConstraints[b].Add(Not(constr));
                            Visit(b);
                        };
                    }
                    buff.Add(c);
                    Visit(c);
                }
                else if (c.Node.Type == NodeType.Assigment) {
                    var key = Convert.ToInt32(Visit(c));
                    if (!Data.ContainsKey(key)) {
                        Data.Add(key, new HashSet<AST>());
                    }
                    Data[key].Add(c);
                    buff.Add(c);
                    if (!PathConstraints.ContainsKey(c)) {
                        PathConstraints.Add(c, new HashSet<String>());
                    }

                    PathConstraints[c].UnionWith(PathConstraints[ast]);
                }
            }

            return "";
        }

        public override String IfVisitor(AST ast)
        {
            if (!PathConstraints.ContainsKey(ast)) {
                PathConstraints.Add(ast, new HashSet<String>());
            }

            PathConstraints[ast].UnionWith(PathConstraints[ast.Parent]);
            PathConstraints[ast].Add(
                Visit(ast.Children[0])
            );
            Visit(ast.Children[1]);
            return "";
        }

        public override String DeclarationVisitor(AST ast)
        {
            return "";
        }

        public override String AssigmentVisitor(AST ast)
        {
            return Visit(ast.Children[1]);
        }

        public override String LookupVisitor(AST ast)
        {
            return Visit(ast.Children[1]);
        }

        public override String VarNameVisitor(AST ast)
        {
            return "";
        }

        public override String NumberVisitor(AST ast)
        {
           return ast.Node.Value;
        }

        public override String SinkVisitor(AST ast)
        {
            return "";
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
            return Type == TokenType.Space;
        }

        public bool isIdentifier() {
            return Type == TokenType.Identifier;
        }

        public bool isNumber() {
            return Type == TokenType.Number;
        }

        public bool isConcatable() {
            return (
                isSpace() || 
                (isIdentifier() && 
                !(new Regex(@"\d+$").IsMatch(Value))) ||
                isNumber()
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
            Prepare();
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
            Source = (
                Source
                    .SkipWhile(t => t.Type != TokenType.LBracket)
                    .SkipWhile(t => t.Type == TokenType.LBracket)
                    .SkipWhile(t => t.Type != TokenType.LBracket)
                    .Where(t => !uselessTokens.Contains(t.Type))
            );
            if (Config.Debug) {
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
            return ParseStart(Source);
        }

        private AST ParseStart(IEnumerable<Token> stream) {
            AST current = Ast;
            var tokens = stream.GetEnumerator();
            if (Next(tokens).Type == TokenType.LBracket) {
                return ParseBlock(current, tokens);
            }
            Error("No entry block found");
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
                        Error(
                            String.Format(
                                "{0} ({1}) is not valid Token for block",
                                tok.Type,
                                tok.Value
                            )
                        );
                        break;
                }
            }
            Error("Unexpected stream end");
            return ast;
        }


        private AST ParseSink(AST current, IEnumerator<Token> stream) {
            AST ast = new AST(
                new TreeNode("System.out.println", NodeType.Sink),
                current
            );
            Token token = Next(stream);
            if (token.Type != TokenType.Identifier) {
                Error("There must be varname in sink end");
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
                Error("There must be an identifier in the start of if condition");
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
                        Error("If must have a body block after condition");
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
                    Error("Unexpected token in If block");
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
                Error(
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
                Error("There must be a number in the end of assigment expression");
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
        public List<AST> Children { get; set; }


        public bool isLeaf() {
            return Children == null;
        }

        public bool isRoot() {
            return Parent == null;
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
            if (args.Length == 2 && (args[1] == "-d" || args[1] == "--debug")) {
                Config.Debug = true;
            }

            AST ast = new Parser(
                Lexer.Tokenize(source)
            ).Parse();
            if (Config.Debug) {
                Console.WriteLine(new SExprVisitor().Visit(ast));
            }
            ast = new SimplificationVisior().Visit(ast);
            if (Config.Debug) {
                Console.WriteLine(new SExprVisitor().Visit(ast));
            }

            var visitor = new SAVisitor();
            visitor.Visit(ast);
            Console.WriteLine(
                String.Format(
                    "[{0}]",
                    String.Join(
                        ", ",
                        visitor.PossibleValues()
                    )
                )
            );

            return 0;
        }
    }
}