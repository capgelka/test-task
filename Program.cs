using System;
using System.IO;
using Antlr4.Runtime;


namespace test_task
{
    static class Programm
    {
        public static int Main(string[] args)
        {

            StreamReader sr;
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
                sr = new StreamReader(args[0]);
            }
            catch (Exception e)
            {
                Console.WriteLine("The file could not be read:");
                Console.WriteLine(e.Message);
                return 2;
            }
            AntlrInputStream inputStream = new AntlrInputStream(sr);
            JavaLexer javaLexer = new JavaLexer(inputStream);            
            CommonTokenStream commonTokenStream = new CommonTokenStream(javaLexer);
            JavaParser JavaParser = new JavaParser(commonTokenStream);

            JavaParser.ExpressionContext expressionContext = javaParser.expression();
            JavaVisitor visitor = new JavaVisitor();

            Console.WriteLine(visitor.Visit(expressionContext));
            sr.Close();
            return 0;
        }
    }
}