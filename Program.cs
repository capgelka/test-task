using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;

using System.Threading.Tasks;

using Decider.Csp.BaseTypes;
using Decider.Csp.Integer;
using Decider.Csp.Global;


static class Constants
{
    public const bool Debug = false;
}



namespace test_task
{   

    // public class ClonableVar : VariableInteger
    // {
    //     public ClonableVar Clone ()
    //     {
    //         return this.MemberberwiseClone();
    //     }
    // }

    public class SATSolver
    {

        public SATSolver()
        {
            Constraints = new HashSet<ExpressionInteger>();
            Vars = new Dictionary<String, VariableInteger>();
        }

        public HashSet<ExpressionInteger> Constraints { get; private set;}
        public Dictionary<String, VariableInteger> Vars { get; }

        public VariableInteger AddVariable(String name)
        {
            if (Vars.ContainsKey(name)) {
                return Vars[name];
            }
            var vi = new VariableInteger(name, 0, 1);
            // Console.WriteLine("Create variable {0} {1}", vi, vi.Name);
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
            Console.WriteLine("Add constraint {0}", constr);
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
            if (searchResult == StateOperationResult.Solved) {
                Console.WriteLine("**Solution***");
                foreach (var v in vars) {
                    Console.WriteLine("{0} {1}", v.Name, v);
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
                string.Join("\n", ast.Children.Select(Visit))
            );
        }

        public override String IfVisitor(AST ast)
        {
            return String.Format(
                "(if ({0}) {1})",
                this.Visit(ast.Children[0]),
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

    public class State
    {
        public State()
        {
            Info = "";
            Vars = new Dictionary<String, VariableInteger>();
            Data = new Dictionary<int, HashSet<ExpressionInteger>>();
            PathConstraints = new HashSet<ExpressionInteger>();

            Solver = new SATSolver();
        }  

        private Dictionary<int, HashSet<ExpressionInteger>> Data { get; set; }
        private Dictionary<String, VariableInteger> Vars { get; set; }

        public String Info { get; set; }
        public SATSolver Solver = new SATSolver();
        public HashSet<ExpressionInteger> PathConstraints { get; }

        public void AddVar(int value)
        {
            Data.Add(value, new HashSet<ExpressionInteger>());
            if (PathConstraints.Count() > 0) {
                foreach (var cons in PathConstraints) {
                    Data[value].Add(cons);
                }
                // var constraint = PathConstraints.Aggregate(
                //     (acc, el) => Solver.And(acc, el)
                // );
                // foreach (var k in Data.Keys.Where(x => x != value)) {
                //     Console.WriteLine("Adding constr for x={0} {1}", value, k);
                //     Data[k].Add(Solver.Not(constraint));
                // }
            }
        }

        // public void Show()
        // {
        //     Console.WriteLine(
        //         "Vars: {0}",
        //         String.Join(" ", Vars.Values.Select(v => v.Name))
        //     );
        //     // Console.WriteLine(
        //     //     "Data {0}",
        //     //     // String.Join("\n",
        //     //         Data.Keys.Select(
        //     //             k => String.Format(
        //     //                 "{k} {0}",
        //     //                 Data[k].Select(
        //     //                     e => String.Format("{0}", e)
        //     //                 )
        //     //             )
        //     //         // )
        //     //     )
        //     // );
        //     Console.WriteLine("PathConstraints:");
        //     Console.WriteLine(String.Join("\n", PathConstraints));
        // }

        public State Update(State other)
        {
            foreach (var k in other.Vars.Keys){
                // Console.WriteLine("Copy var {0}", k);
                Vars.Add(k, other.Vars[k]);
            }
            foreach(var k in other.Data.Keys) {
                if (!Data.ContainsKey(k)) {
                    this.AddVar(k);
                    foreach(var c in other.Data[k]) {
                        Data[k].Add(c);
                    }
                }
                else {
                    // CspTerm tmp = Solver.True;
                    // foreach(var c in other.Data[k]) {
                    //     tmp = Solver.And(tmp, c);
                    // }
                    var current = Data[k].Aggregate(
                        (acc, el) => Solver.And(acc, el)
                    );
                    var another = other.Data[k].Aggregate(
                        (acc, el) => Solver.And(acc, el)
                    );
                    Data[k] = new HashSet<ExpressionInteger> { 
                        Solver.Or(current, another)
                    };
                }            
            }
            foreach (var k in Data.Keys.Where(x => !other.Data.ContainsKey(x))) {
                var constraint = other.PathConstraints.Aggregate(
                    (acc, el) => Solver.And(acc, el)
                );
                // Data[k] = HashSet<ExpressionInteger> { Solver.And()}
                Data[k].Add(Solver.Not(constraint));
            }
            //Show();
            return this;
        }

        public State UpdateOnConstraint(String cName)
        {
            if (!Vars.ContainsKey(cName)) {
                // Console.WriteLine("adding var {0}", cName);
                Vars.Add(cName, Solver.AddVariable(cName));
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
            Console.WriteLine("-------Solving constraints-------");
            // Show();
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
                    //Solver.AddConstraint(cons);
                    Console.WriteLine(cons.Integer);
                }


                // foreach (var x in Vars.Values) {
                //     Console.WriteLine("---");
                //     Console.WriteLine(x.Name);
                // }


                // var task = Task<ConstraintSolverSolution>.Factory.StartNew(() => Solver.Solve());
                // task.Wait();
                // var solution = task.Result;
                // var varsCopy = new List<VariableInteger>();
                // foreach (var v in Vars.Values) {
                //     varsCopy.Add((ClonableVar) v.Clone());
                // }

                if (Solver.Solve(Vars.Values, Data[k])) {

                    // foreach (var val in Vars.Keys) {
                    //     Console.WriteLine("=_=");
                    //     //Console.WriteLine(solution[key]);
                    // }

                    // foreach (var condKey in Vars.Keys) {
                    //     Console.WriteLine("!!!");
                    //     Console.WriteLine("condition[{0}]: {1}", condKey, Vars[condKey]);
                    //     try {
                    //         Console.WriteLine(solution[Vars[condKey]]);
                    //     }

                    //     catch {
                    //         Console.WriteLine("(");
                    //     }
                    // }

                    buff.Add(k);
                }
                // Solver.RemoveConstraints();
            }
            return buff;
        }

    }

    public class SAVisitor : IVisitor<String>
    {
        public SAVisitor()
        {
            // CurrentState = new State();
            PathConstraints = new Dictionary<AST, HashSet<String>>();
            Data = new Dictionary<int, HashSet<AST>>();
        }   

        // private State CurrentState {get; set; }
        private Dictionary<AST, HashSet<String>> PathConstraints {get; set; }
        private Dictionary<int, HashSet<AST>> Data {get; set; }

        private String VarName(String name)
        {
            if (name.StartsWith("!")) {
                return name.Substring(1);
            }
            return name;
        }

        public void Show()
        {
            Console.WriteLine("=== PAth Constraints:");
            foreach (var key in PathConstraints.Keys) {
                key.Show();
                Console.WriteLine("Constraints for:");
                foreach (var constr in PathConstraints[key]) {
                    Console.WriteLine(constr);
                }
            }
        }

        private SATSolver PrepareSolver(int value)
        {
            var Solver = new SATSolver();
            List<ExpressionInteger> buff = new List<ExpressionInteger>();
            Console.WriteLine(Data[value]);
            foreach (var key in Data[value]) {
                var tmp = new List<ExpressionInteger>();
                Console.WriteLine(PathConstraints[key]);
                foreach (var c in PathConstraints[key]) {
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
            if (buff.Count() == 0) {
                ;
            }
            else if (buff.Count() == 1) {
                Solver.AddConstraint(buff[0]);
            } else {
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
            Show();
            Console.WriteLine("-------Solving constraints-------");
            // Show();
            foreach (var k in Data.Keys) {
                var Solver = PrepareSolver(k);
                Console.WriteLine("Solving for {0}", k);

                if (Solver.Solve()) {
                    buff.Add(k);
                }
            }
            return buff;
        }

        public override String BlockVisitor(AST ast)
        {
            // State st = new State();
            PathConstraints.Add(ast, new HashSet<String>());
            // PathConstraints[ast].UnionWith(PathConstraints[ast.Parent]);
            // PathConstraints[ast].Add(cons);
            if (!ast.isRoot()) {
                PathConstraints[ast].UnionWith(PathConstraints[ast.Parent]);
            }
            var buff = new HashSet<AST>();
            var isLeaf = true;
            foreach(var c in ast.Children) {
                var constr = Visit(c.Children[0]);
                if (c.Node.Type == NodeType.If) {
                    isLeaf = false;
                    if (buff.Count() > 0) {
                        foreach(var b in buff) {
                            // PathConstraints.Add(c, new HashSet<String>());
                            // PathConstraints[b].UnionWith(PathConstraints[ast]);
                            PathConstraints[b].Add("!" + constr);
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
                    PathConstraints.Add(c, new HashSet<String>());
                    PathConstraints[c].UnionWith(PathConstraints[ast]);
              
                    
                    // PathConstraints.UnionWith(PathConstraints[ast]);
                    // st.Update(s);
                }
            }
            // ast.Show();
            // Console.WriteLine(
            //     "Constraints {1} {0}",
            //     st.PathConstraints.Count(),
            //     String.Join(" ", st.PathConstraints)
            // );
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
        public List<AST> Children { get; set; }


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
            ast = new SimplificationVisior().Visit(ast);
            Console.WriteLine(new SExprVisitor().Visit(ast));

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


            var s = new SATSolver();
            var a = s.AddVariable("a");
            var b = s.AddVariable("b");
            var c = s.AddVariable("c");
            var d = s.AddVariable("d");
            var d2 = s.AddVariable("d");
            Console.WriteLine("UUUUU {0}", d == d2);
            foreach (var v in s.Vars.Values) {
                Console.WriteLine(v.Name);
            }

            s.AddConstraint(a);
            s.AddConstraint(s.And(b, c));
            s.AddConstraint(s.Or(s.Not(a), c));

            // Console.WriteLine(s.Or(s.Not(a), c).Integer);
            Console.WriteLine(s.Solve());

            return 0;
        }
    }
}