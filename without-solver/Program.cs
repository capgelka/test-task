using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;


namespace test_task
{   

    public class Config
    {
        public static bool Debug = false;
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

    public class PathVisitor : IVisitor<int>
    {

        public PathVisitor()
        {
            NodeMapping = new Dictionary<AST, int>();
            PathConstraints = new List<HashSet<int>>();
            Data = new Dictionary<int, HashSet<AST>>();
        }   

        // we need Node mapping and pathconstraits separate to save order
        // in which we have visited nodes
        private Dictionary<AST, int> NodeMapping {get; set; }
        private Dictionary<int, HashSet<AST>> Data {get; set; }
        private List<HashSet<int>> PathConstraints {get; set; }

        private void RemoveUnreachable()
        {
            foreach (var ast in NodeMapping.Keys) {
                if (ast.Node.Type != NodeType.Assigment) {
                    PathConstraints[NodeMapping[ast]] = null;
                }
            }
            for (int i = 0; i < PathConstraints.Count(); i++) {
                if (PathConstraints[i] == null) {
                    continue;
                }
                for (int j = i + 1; j < PathConstraints.Count(); j++) {
                    if (PathConstraints[j] == null) {
                        continue;
                    }
                    if (PathConstraints[i].IsSubsetOf(PathConstraints[j])) {
                        PathConstraints[j] = null;
                    }
                }
            }
        }
  

        public List<int> PossibleValues()
        {   
            RemoveUnreachable();
            var buff = new List<int>();
            foreach (var k in Data.Keys) {
                foreach (var ast in Data[k]) {
                    if (PathConstraints[NodeMapping[ast]] != null) {
                        buff.Add(k);
                        break;
                    }
                }
            }
            buff.Reverse(); // just to see vars in the order they are in original programm
            return buff;
        }

        private HashSet<int> ParentConstraints(AST ast)
        {
            return PathConstraints[NodeMapping[ast.Parent]];
        }

        private void InitNode(AST ast)
        {
            if (!NodeMapping.ContainsKey(ast)) {
                PathConstraints.Add(new HashSet<int>());
                NodeMapping.Add(ast, PathConstraints.Count() - 1);
            }
            
            if (!ast.isRoot()) {
                PathConstraints.Last().UnionWith(
                    ParentConstraints(ast)
                );
            }
        }

        private void AddConstraint(AST ast, int constr)
        {
            PathConstraints.Last().Add(constr);
        }

        public override int BlockVisitor(AST ast)
        {   
            InitNode(ast);
            ast.Children.Reverse();
            foreach(var c in ast.Children) {
                if (c.Node.Type == NodeType.If) {
                    Visit(c);
                }
                else if (c.Node.Type == NodeType.Assigment) {
                    var key = Visit(c);
                    InitNode(c);
                    if (!Data.ContainsKey(key)) {
                        Data.Add(key, new HashSet<AST>());
                    }
                    Data[key].Add(c);
                }
            }
            ast.Children.Reverse();
            return 0;
        }

        public override int IfVisitor(AST ast)
        {
            InitNode(ast);
            AddConstraint(
                ast,
                Visit(ast.Children[0])
            );
            Visit(ast.Children[1]);
            return 0;
        }

        public override int DeclarationVisitor(AST ast)
        {
            return 0;
        }

        public override int AssigmentVisitor(AST ast)
        {
            return Visit(ast.Children[1]);
        }

        public override int LookupVisitor(AST ast)
        {
            return Visit(ast.Children[1]);
        }

        public override int VarNameVisitor(AST ast)
        {
            return 0;
        }

        public override int NumberVisitor(AST ast)
        {
            return Convert.ToInt32(ast.Node.Value);
        }

        public override int SinkVisitor(AST ast)
        {
            return 0;
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
 
            var visitor = new PathVisitor();
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