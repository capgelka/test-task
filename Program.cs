using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;

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

    public class State
    {
        public State()
        {
            TreeNode Node = null;
            String Info = "";
            Dictionary<int, HashSet<int>> Positive = new Dictionary<int, HashSet<int>>();
            Dictionary<int, HashSet<int>> Negative = new Dictionary<int, HashSet<int>>();
        }  

        private Dictionary<int, HashSet<int>> Positive { get; set; }
        private Dictionary<int, HashSet<int>> Negative { get; set; }
        public TreeNode Node { get; set; }
        public String Info { get; set; }

        public void AddVar(int var)
        {
            Positive.Add(var, new HashSet<int>());
        }

        public void AddConstraint(int var, int constraint)
        {
            Positive[var].Add(constraint);
        }

        public void Update(State other)
        {
            foreach(var k in other.Positive.Keys) {
                if (!Positive.ContainsKey(k)) {
                    AddVar(k);
                    Positive[k].UnionWith(other.Positive[k]);

                }
                else {
                    foreach(var pk in Positive.Keys) {
                        if (!Negative.ContainsKey(pk) && pk != k) {
                            Negative.Add(pk, other.Positive[k]);
                        }
                    }
                    // Positive[k].
                }
                // AddConstraint(k, other[k]);
            }
        }

        // public static State Join(IEnumerable<State> states)
        // {

        // }


    }

    public class SAVisitor : IVisitor<State>
    {
        // SAVisitor()
        // {
        //     State St = new St();
        // }

        // private State St { get; set; }

        public override State BlockVisitor(AST ast)
        {
            State st = new State();
            foreach(State a in ast.Children.Select(a => this.Visit(a))) {
                st.Update(a);
            }
            return st;
        }

        public override State IfVisitor(AST ast)
        {
            int constraint = Convert.ToInt32(Visit(ast.Children[0]).Info);
            Visit(ast.Children[1]);
            return new State();
        }

        // don't need this
        public override State DeclarationVisitor(AST ast)
        {
            return null;
        }

        public override State AssigmentVisitor(AST ast)
        {
            State st = Visit(ast.Children[1]);
            st.Node = new TreeNode("", NodeType.Assigment);
            return st;
        }

        public override State LookupVisitor(AST ast)
        {
            return Visit(ast.Children[1]);
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
            // foreach (Token tok in Source) {
            //     Console.WriteLine(
            //         String.Format("{0} {1}", tok.Type, tok.Value)
            //     );
            //}
        }

        // private void dumpParents(AST node) {
        //     while (!node.isRoot()) {
        //         Console.WriteLine(
        //             node.Node.Type + 
        //             ": <" + 
        //             string.Join(", ", node.Children.Select(
        //                 s => (s.Node.Type + " (\"" + s.Node.Value + "\")"))
        //             ) + 
        //             ">"
        //         );

        //         node = node.Parent;
        //     }
        // }

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

        public void Show() {
            Console.WriteLine(
                Node.Type + 
                ": <" + 
                string.Join(", ", Children.Select(
                    s => (s.Node.Type + " (\"" + s.Node.Value + "\")"))
                ) + 
                ">"
            );
        }


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
            {   // Open the text file using a stream reader.
                // using (StreamReader sr = new StreamReader(args[0]))
                // {
                // // Read the stream to a string, and write the string to the console.
                //     String line = sr.ReadToEnd();
                //     Console.WriteLine(line);
                // }
                // sr = new StreamReader(args[0]);
                source = System.IO.File.ReadAllText(args[0]);
            }
            catch (Exception e)
            {
                Console.WriteLine("The file could not be read:");
                Console.WriteLine(e.Message);
                return 2;
            }
            // AntlrInputStream inputStream = new AntlrInputStream(sr);
            // JavaLexer javaLexer = new JavaLexer(inputStream);            
            // CommonTokenStream commonTokenStream = new CommonTokenStream(javaLexer);
            // JavaParser javaParser = new JavaParser(commonTokenStream);

            // JavaParser.ExpressionContext expressionContext = javaParser.expression();
            // JavaParserBaseVisitor visitor = new JavaParserBaseVisitor();

            // Console.WriteLine(visitor.Visit(expressionContext));
            // foreach (char letter in source) {
            //      Console.Write(letter);
            // }

            List<Token> tokens = Lexer.Tokenize(source);
            // List<Token> tokens = Lexer.Tokenize(source).Aggregate(
            //     new List<Token>(),
            //     (acc, val)
            //      => {
            //         //Console.Write("-->>>" + acc.Last().Value + " " + acc.Last().isSpace() +"\n");
            //         if (!(val.isSpace() && acc.Count() > 0 && acc.Last().isSpace())) {
            //             acc.Add(val);
            //         }
            //         return acc;
            //     } 
            // );
            // foreach (Token tok in tokens) {
            //      Console.Write(tok.Type + " " + tok.Value + "\n");
            // }
            AST ast = new Parser(tokens).Parse();
            Console.WriteLine(new SExprVisitor().Visit(ast));
            return 0;
        }
    }
}