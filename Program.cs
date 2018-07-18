using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;
// using Antlr4.Runtime;


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
        public T Visit(AST ast)
        {
            Console.WriteLine("Call on {0}, depth {1}", ast.Node.Type, ast.Depth());
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
                default:
                    Console.WriteLine("WTF?");
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
    }


    public class SExprVisitor : IVisitor<String>
    {

        public override String BlockVisitor(AST ast)
        {
            return String.Format(
                "({0})",
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
            Console.WriteLine("{0}", (ast.Children[0].Node ?? new TreeNode("OLOLO", NodeType.Block)).Value);
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
                string.Join("", ast.Children.Select(a => this.Visit(a)))
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


    }

    public class Token
    {
        public Token(string value, TokenType token) {
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


        public AST Ast { get; set; }
        public IEnumerable<Token> Source { get; set; }

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
            // foreach (Token tok in this.Source) {
            //      Console.Write(tok.Type + " " + tok.Value + "\n");
            // }
        }

        private void dumpParents(AST node) {
            while (!node.isRoot()) {
                Console.WriteLine(
                    node.Node.Type + 
                    ": <" + 
                    string.Join(", ", node.Children.Select(
                        s => (s.Node.Type + " (\"" + s.Node.Value + "\")"))
                    ) + 
                    ">"
                );

                node = node.Parent;
            }
        }

        private void Error(string Message) {
            Console.WriteLine("Parse error {0}", Message);
            Environment.Exit(3);
        }

        public AST Parse() {
            return ParseStart(this.Source);
        }

        private AST ParseStart(IEnumerable<Token> stream) {
            AST current = this.Ast;
            Token tok = stream.Take(1).Last();
            var tok2 = stream.Take(1).Last();
            Console.WriteLine("{0} {1}", tok.Type, tok.Value);
            Console.WriteLine("{0} {1}", tok2.Type, tok2.Value);
            if (tok.Type == TokenType.LBracket) {
                stream = stream.Skip(1);
                return this.ParseBlock(current, stream);
            }
            this.Error("No entry block found");
            return null; // Stupid compiler
        }

        private AST ParseBlock(AST current, IEnumerable<Token> stream) {
            // var tokens = stream.GetEnumerator();
            // Token current;
            // int counter = 0;
            // while (tokens.MoveNext()) {
            //     current = tokens.Current;
            current.Node = new TreeNode("Block", NodeType.Block);
            foreach(Token tok in stream) {
                switch (tok.Type)
                {   
                    case TokenType.IntType:
                        current.Children.Add(
                            ParseDeclaration(current, stream)
                        );
                        break;
                    case TokenType.Identifier:
                        current.Children.Add(
                            ParseAssigment(current, tok, stream)
                        );
                        break;
                    case TokenType.If:
                        current.Children.Add(
                            ParseIf(current, stream)
                        );
                        break;
                    case TokenType.Sink:
                        current.Children.Add(
                            ParseSink(current, stream)
                        );
                        break;
                    case TokenType.RBracket:
                        return current;
                    default:
                        this.Error(
                            String.Format("{0} is not valid Token for block", tok.Type)
                        );
                        break;
                }
                stream = stream.Skip(1);
            }
            this.Error("Unexpected stream end");
            return current;
        }


        private AST ParseSink(AST current, IEnumerable<Token> stream) {
            AST ast = new AST(
                new TreeNode("System.out.println", NodeType.Sink),
                current
            );
            Token token = stream.Take(1).Last();
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


        private AST ParseIf(AST current, IEnumerable<Token> stream) {
            Token tok = stream.Take(1).Last();
            if (tok.Type != TokenType.Identifier) {
                this.Error("There must be an identifier in the start of if condition");
            }
            AST ast = new AST(
                new TreeNode("", NodeType.If),
                current
            );
            Token nextToken = stream.Take(1).Last();
            switch(nextToken.Type)
            {
                case TokenType.Number:
                    ast.Children.Add(
                        ParseLookup(ast, tok, nextToken)
                    );
                    if (stream.Take(1).Last().Type != TokenType.LBracket) {
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

        private AST ParseDeclaration(AST current, IEnumerable<Token> stream) {
            Token token = stream.Take(1).Last();

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

        private AST ParseAssigment(AST current, Token token, IEnumerable<Token> stream) {
            Token nextToken = stream.Take(1).Last();

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

        private AST BadParse() {
            AST current = this.Ast;
            AST tmp = null;
            TreeNode tmpNode = null;
            foreach(Token token in this.Source) {
                Console.Write(token.Type + " " + token.Value + " " + current.Depth() + "\n");
                switch(token.Type)
                {
                    case TokenType.LBracket:
                        if (current.Node != null && current.Node.Type == NodeType.If) {
                            tmp = new AST(current);
                            current.Children.Add(tmp);
                            current = tmp;
                        }
                        current.Node = new TreeNode(token.Value, NodeType.Block);
                        tmp = new AST(current);
                        current.Children.Add(tmp);
                        current = tmp;
                        break;
                    case TokenType.RBracket:
                        if (current.isRoot()) {
                            // Console.WriteLine("Parsing Error. No mathing bracket");
                            // Environment.Error(3);
                            break;
                        }
                        current = current.Parent;
                        if (!current.isRoot() && current.Parent.Node.Type == NodeType.If) {
                            current = current.Parent;
                        }
                        break;
                    case TokenType.If:
                        if (current == null ) {
                            Console.WriteLine("WTF?");
                        }
                        current.Node = new TreeNode(token.Value, NodeType.If);
                        tmp = new AST(current);
                        current.Children.Add(tmp);
                        current = tmp;
                        break;
                    case TokenType.LParentheses:
                        break;
                    case TokenType.RParentheses:
                        current = current.Parent;
                        break;
                    case TokenType.Semicolon:
                        // if (current.Node.Type == NodeType.Declaration) {
                        //     current = current.Parent;
                        // }
                        if (current.isRoot()) {
                            Console.WriteLine("Semicolon can't be in root");
                            this.dumpParents(current);
                            break;
                            Environment.Exit(3);
                        }
                        current = current.Parent;
                        tmp = new AST(current);
                        current.Children.Add(tmp);
                        current = tmp;
                        break;
                    case TokenType.IntType:
                        if (current.Node != null) {
                            Console.WriteLine("Unexpected declaration");
                            this.dumpParents(current);
                            Environment.Exit(3);
                        }
                        current.Node = new TreeNode("", NodeType.Declaration);
                        // tmp = new AST(current);
                        // current.Children.Add(tmp);
                        break;
                    case TokenType.Identifier:
                        if (current.Node != null &&
                            current.Node.Type == NodeType.Declaration) {
                            // tmpNode = current.Node;
                            // current.Node = new TreeNode(null, NodeType.Declaration);
                            // tmp = new AST(current);
                            // tmp.Node = tmpNode;
                            // current.Children.Add(tmp);
                            tmp = new AST(current);
                            tmp.Node = new TreeNode(token.Value, NodeType.VarName);
                            current.Children.Add(tmp);
                        // } 
                        // else if (
                        //     current.Node != null && current.Node != null &&
                        //     current.Node.Parent.Type == NodeType.If
                        // ) {

                        } else {
                            current.Node = new TreeNode(token.Value, NodeType.VarName);
                            Console.WriteLine("THIS {0}", current.Node.Type);
                        }

                        // if (
                        //     current.Node != null && current.Node != null &&
                        //     current.Node.Parent.Type == NodeType.If
                        // ) {
                        //     current.
                        // }
                        break;
                    case TokenType.Equals:
                        if (current.Node.Type != NodeType.VarName) {
                            Console.WriteLine("Equality not after varname " + current.Node.Type);
                            this.dumpParents(current);
                            Environment.Exit(3);
                        }
                        tmpNode = current.Node;
                        current.Node = new TreeNode(null, NodeType.Assigment);
                        tmp = new AST(current);
                        tmp.Node = tmpNode;
                        current.Children.Add(tmp);
                        // tmp = new AST(current);
                        // tmp.Node = new TreeNode(token.Value, NodeType.VarName);
                        break;
                    case TokenType.RSquareBracket:
                        //current = current.Parent;
                        break;
                    case TokenType.LSquareBracket:
                        if (current.Node.Type != NodeType.VarName &&
                            (current.Children.Count() > 0 && 
                                current.Children.Last().Node.Type != NodeType.VarName)
                            ) {
                            
                            Console.WriteLine(
                                "Square bracket not after identifier {0}",
                                current.Node.Type
                            );
                            Environment.Exit(3);
                        }
                        tmpNode = current.Node;
                        current.Node = new TreeNode(null, NodeType.Lookup);
                        tmp = new AST(current);
                        tmp.Node = tmpNode;
                        current.Children.Add(tmp);
                        break;
                    case TokenType.Number:

                        if (current.Node.Type == NodeType.Lookup || 
                            current.Node.Type == NodeType.Assigment ) {
                            
                            tmp = new AST(current);
                            tmp.Node = new TreeNode(token.Value, NodeType.Number);
                            current.Children.Add(tmp);
                        } else {
                            Console.WriteLine("Unexpected number");
                            this.dumpParents(current);
                            Environment.Exit(3);
                        }
                        break;
                    default:
                        break;
                }
            }
            return this.Ast;
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