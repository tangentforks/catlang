using System;
using System.IO;
using System.Collections.Generic;
using System.Text;

namespace Cat {
    public class CatMetaCommands {
        public class Help : PrimitiveFunction {
            public Help()
                : base( "#help", "( ~> )", "outputs a help message", "meta" ) { }

            public override void Eval( Executor exec ) {
                Output.WriteLine( "Some basic commands to get you started:" );
                Output.WriteLine( "  \"filename\" #load - loads a source file" );
                Output.WriteLine( "  [...] #trace - runs a function in trace mode" );
                Output.WriteLine( "  #test_all - runs all unit tests" );
                Output.WriteLine( "  [...] #type - shows the type of a function" );
                Output.WriteLine( "  [...] #metacat - optimizes a function by applying rewriting rules" );
                Output.WriteLine( "  #defs - lists all loaded commands" );
                Output.WriteLine( "  \"instruction\" #def - detailed help on an instruction" );
                Output.WriteLine( "  \"prefix\" #defmatch - describes all instructions starting with prefix" );
                Output.WriteLine( "  \"tag\" #deftag - outputs all instructions tagged with given tag" );
                Output.WriteLine( "  #exit - exits the Cat interpreter" );
                Output.WriteLine( "" );
            }
        }

        public class MakeHelp : PrimitiveFunction {
            public MakeHelp()
                : base( "#make_help", "( ~> )", "generates an HTML help file", "meta" ) { }

            void OutputTaggedTable( StreamWriter sw, string sTag, Executor exec ) {
                sw.WriteLine( "<table width='100%'>" );
                int nRow = 0;
                foreach (Function f in exec.GetAllFunctions()) {
                    if ((sTag.Length == 0 && f.GetRawTags().Length == 0) || (sTag.Length != 0 && f.GetRawTags().Contains( sTag ))) {
                        if (nRow % 5 == 0)
                            sw.WriteLine( "<tr valign='top'>" );
                        string sName = Util.ToHtml( f.GetName() );
                        string s = "<td><a href='#" + sName + "'>" + sName + "</a></td>";
                        sw.WriteLine( s );
                        if (nRow++ % 5 == 4)
                            sw.WriteLine( "</tr>" );
                    }
                }
                if (nRow % 5 != 0) sw.WriteLine( "</tr>" );
                sw.WriteLine( "</table>" );
            }

            void OutputAllTable( StreamWriter sw, Executor exec ) {
                sw.WriteLine( "<table width='100%'>" );
                int nRow = 0;
                foreach (Function f in exec.GetAllFunctions()) {
                    if (nRow % 5 == 0)
                        sw.WriteLine( "<tr valign='top'>" );
                    string sName = Util.ToHtml( f.GetName() );
                    string s = "<td><a href='#" + sName + "'>" + sName + "</a></td>";
                    sw.WriteLine( s );
                    if (nRow++ % 5 == 4)
                        sw.WriteLine( "</tr>" );
                }
                if (nRow % 5 != 0) sw.WriteLine( "</tr>" );
                sw.WriteLine( "</table>" );
            }

            public override void Eval( Executor exec ) {
                string sHelpFile = Config.gsDataFolder + "\\help.html";
                StreamWriter sw = new StreamWriter( sHelpFile );
                sw.WriteLine( "<html><head><title>Cat Help File</title></head><body>" );

                /*
                sw.WriteLine("<h1><a name='level0prims'></a>Level 0 Primitives</h1>");
                OutputTable(sw, "level0", exec);
                sw.WriteLine("<h1><a name='level1prims'></a>Level 1 Primitives</h1>");
                OutputTable(sw, "level1", exec);               
                sw.WriteLine("<h1><a name='level2prims'></a>Level 2 Primitives</h1>");
                OutputTable(sw, "level2", exec);                
                sw.WriteLine("<h1><a name='otherprims'></a>Other Functions</h1>");
                OutputTable(sw, "", exec);
                 */

                sw.WriteLine( "<h1>Instructions</h1>" );
                OutputAllTable( sw, exec );

                sw.WriteLine( "<h1>Definitions</h1>" );
                sw.WriteLine( "<pre>" );
                foreach (Function f in exec.GetAllFunctions()) {
                    sw.WriteLine( f.GetImplString( true ) );
                }
                sw.WriteLine( "</pre>" );

                sw.WriteLine( "</body></html>" );
                sw.Close();
                Output.WriteLine( "saved help file to " + sHelpFile );
            }
        }

        public class DefTags : PrimitiveFunction {
            public DefTags()
                : base( "#deftags", "( ~> )", "outputs a list of the different tags associated with the functions" ) { }

            public override void Eval( Executor exec ) {
                Dictionary<string, List<Function>> taggedFxns = exec.GetFunctionsByTag();
                List<string> tags = new List<string>();
                foreach (string s in taggedFxns.Keys)
                    tags.Add( s );
                tags.Sort();
                foreach (string s in tags)
                    Output.WriteLine( s );
            }
        }

        public class DefTag : PrimitiveFunction {
            public DefTag()
                : base( "#deftag", "(string ~> )", "outputs a list of the functions associated with a tag" ) { }

            public override void Eval( Executor exec ) {
                Dictionary<string, List<Function>> taggedFxns = exec.GetFunctionsByTag();
                string s = exec.PopString();
                if (!taggedFxns.ContainsKey( s )) {
                    Output.WriteLine( "no functions are tagged '" + s + "'" );
                } else {
                    foreach (Function f in taggedFxns[ s ])
                        Output.Write( f.GetName() + "\t" );
                    Output.WriteLine( "" );
                }
            }
        }

        public class Load : PrimitiveFunction {
            public Load()
                : base( "#load", "(string ~> )", "loads and executes a source code file", "meta" ) { }

            public override void Eval( Executor exec ) {
                exec.LoadModule( exec.PopString() );
            }
        }

        public class Save : PrimitiveFunction {
            public Save()
                : base( "#save", "(string ~> )", "saves a transcript of the session so far", "meta" ) { }

            public override void Eval( Executor exec ) {
                MainClass.SaveTranscript( exec.PopString() );
            }
        }

        public class TypeOf : PrimitiveFunction {
            public TypeOf()
                : base( "#type", "(function -> function)", "displays the type of an expression", "meta" ) { }

            public override void Eval( Executor exec ) {
                QuotedFunction f = exec.TypedPeek<QuotedFunction>();
                bool bVerbose = Config.gbVerboseInference;
                bool bInfer = Config.gbTypeChecking;
                Config.gbVerboseInference = true;
                Config.gbTypeChecking = true;
                try {
                    CatFxnType ft = CatTypeReconstructor.Infer( f.GetSubFxns() );
                    if (ft == null)
                        Output.WriteLine( "type could not be inferred" );
                } finally {
                    Config.gbVerboseInference = bVerbose;
                    Config.gbTypeChecking = bInfer;
                }
            }
        }

        public class Expand : PrimitiveFunction {
            public Expand()
                : base( "#inline", "(('A -> 'B) ~> ('A -> 'B))", "performs inline expansion", "meta" ) { }

            public override void Eval( Executor exec ) {
                QuotedFunction qf = exec.PopFxn();
                exec.Push( Optimizer.ExpandInline( qf, 5 ) );
            }
        }

        public class Expand1 : PrimitiveFunction {
            public Expand1()
                : base( "#inline-step", "(('A -> 'B) -> ('A -> 'B))", "performs inline expansion", "meta" ) { }

            public override void Eval( Executor exec ) {
                QuotedFunction qf = exec.PopFxn();
                exec.Push( Optimizer.ExpandInline( qf, 1 ) );
            }
        }

        public class ApplyMacros : PrimitiveFunction {
            public ApplyMacros()
                : base( "#metacat", "(('A -> 'B) ~> ('A -> 'B))", "runs MetaCat rewriting rules", "meta" ) { }

            public override void Eval( Executor exec ) {
                QuotedFunction qf = exec.PopFxn();
                exec.Push( Optimizer.ApplyMacros( exec, qf ) );
            }
        }

        public class Clr : PrimitiveFunction {
            public Clr()
                : base( "#clr", "('A ~> )", "removes all items from the stack", "meta" ) { }

            public override void Eval( Executor exec ) {
                exec.Clear();
            }
        }

        public class MetaData : PrimitiveFunction {
            public MetaData()
                : base( "#metadata", "(('A -> 'B) ~> ('A -> 'B) list)", "outputs the meta-data associated with a function", "meta" ) { }

            public override void Eval( Executor exec ) {
                QuotedFunction f = exec.TypedPeek<QuotedFunction>();

                if (f.GetMetaData() != null)
                    exec.Push( f.GetMetaData().ToList() );
                else
                    exec.Push( new CatList() );
            }
        }

        public class Test : PrimitiveFunction {
            public Test()
                : base( "#test", "(('A -> 'B) ~> ('A -> 'B))", "runs the unit tests associated with an instruction", "meta" ) { }

            public override void Eval( Executor exec ) {
                QuotedFunction f = exec.TypedPeek<QuotedFunction>();
                foreach (Function g in f.GetSubFxns())
                    if (exec.TestFunction( g ))
                        Output.WriteLine( "tests succeeded for " + g.GetName() );
            }
        }

        public class TestAll : PrimitiveFunction {
            public TestAll()
                : base( "#test_all", "( ~> )", "runs all unit tests associated with all instructions", "meta" ) { }

            public override void Eval( Executor exec ) {
                exec.testCount = 0;
                int nFailCount = 0;
                foreach (Function f in exec.GetAllFunctions())
                    if (!exec.TestFunction( f ))
                        nFailCount++;
                Output.WriteLine( "ran " + exec.testCount + " unit tests with " + nFailCount + " failures" );
            }
        }

        public class Defs : PrimitiveFunction {
            public Defs()
                : base( "#defs", "( ~> )", "outputs a complete list of defined instructions ", "meta" ) {
            }

            public override void Eval( Executor exec ) {
                foreach (Function f in exec.GetAllFunctions())
                    Output.Write( f.GetName() + "\t" );
                Output.WriteLine( "" );
            }
        }

        public class DefMatch : PrimitiveFunction {
            public DefMatch()
                : base( "#defmatch", "(string ~> )", "outputs a detailed description of all instructions starting with the name", "meta" ) {
            }

            public override void Eval( Executor exec ) {
                string sName = exec.PopString();
                foreach (Function f in exec.GetAllFunctions())
                    if (f.GetName().IndexOf( sName ) == 0)
                        Output.WriteLine( f.GetImplString() );
            }
        }

        public class Def : PrimitiveFunction {
            public Def()
                : base( "#def", "(string ~> )", "outputs a detailed description of the instruction", "meta" ) {
            }

            public override void Eval( Executor exec ) {
                string sName = exec.PopString();
                Function f = exec.Lookup( sName );
                if (f == null)
                    Output.WriteLine( "instruction '" + sName + "' was not found" );
                else
                    Output.WriteLine( f.GetImplString() );
            }
        }

        public class Trace : PrimitiveFunction {
            public Trace()
                : base( "#trace", "('A ('A -> 'B) ~> 'B)", "used to trace the execution of a function", "meta" ) { }

            public override void Eval( Executor exec ) {
                Function f = exec.PopFxn();
                exec.bTrace = true;
                try {
                    f.Eval( exec );
                } finally {
                    exec.bTrace = false;
                }
            }
        }
    }

}
