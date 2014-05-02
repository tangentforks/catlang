using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

namespace Cat {
    public interface IFunctionGenerator {
        void OpenScope();
        void CloseScope();
        string GenerateLabel();
        void AddArgument( string sName, string sType );
        void AddLocalVar( string sName, string sType );
        void AddFunctionCall( string sName );
        void AddAssignment( string sName );
        void AddLabel( string sLabel );
        void AddJumpStatement( string sLabel );
        void AddJumpIfNotStatement( string sLabel );
        void AddReturnStatement();
        void AddComment( string sComment );
        void AddPushCopy( string sVar );
        void AddRawInstruction( string sOp );
    }

    public enum OpType {
        Comment,
        Call,
        Assignment,
        Jump,
        JumpIfNot,
        Return,
        Copy,
        Raw,
        Label,
    }

    public interface IOperation {
        // An instruction can be used for holding debugging information, or it might contain 
        // somethign useful? 
        string ToString();
        OpType GetOpType();
        string GetOperand();
    }

    /// <summary>
    /// This is a class used for generating sequences of Cat instructions in a manner that is relatively
    /// platform independent. The hope is that we could actually use the same code generator for 
    /// directly generating code in other languages (e.g. MSIL byte-code)
    /// </summary>
    public class CatFunctionGenerator : IFunctionGenerator, IEnumerable<IOperation> {
        #region cat operations
        public class CatOp : IOperation {
            OpType optype;
            string operand;

            public CatOp( OpType optype, string operand ) {
                this.operand = operand;
                this.optype = optype;
            }

            public override string ToString() {
                return this.optype.ToString() + " " + this.operand;
            }

            public OpType GetOpType() {
                return optype;
            }

            public string GetOperand() {
                return operand;
            }
        }
        #endregion

        #region fields
        Stack<Dictionary<string, string>> names = new Stack<Dictionary<string, string>>();
        Dictionary<string, string> vars = new Dictionary<string, string>();
        Dictionary<string, string> args = new Dictionary<string, string>();
        List<string> labels = new List<string>();
        List<CatOp> ops = new List<CatOp>();
        string name;
        #endregion

        #region c'tors / d'tors
        public CatFunctionGenerator( string name ) {
            this.name = name;
            names.Push( new Dictionary<string, string>() );
        }
        #endregion

        #region IFunctionGenerator Members

        public void AddRawInstruction( string sOp ) {
            AddOp( OpType.Raw, sOp );
        }

        public void OpenScope() {
            names.Push( new Dictionary<string, string>() );
        }

        public void CloseScope() {
            names.Pop();
        }

        public void AddArgument( string sName, string sType ) {
            if (names.Count != 1)
                throw new Exception( "You can't add an argument once you have declared a new scope" );
            args.Add( sName, sType );
            if (names.Peek().ContainsKey( sName ))
                throw new Exception( "Name '" + sName + "' has already been declared" );
            names.Peek().Add( sName, sType );
        }

        public void AddLocalVar( string sName, string sType ) {
            string sUniqueName = sName + "$" + vars.Count;
            vars.Add( sUniqueName, sType );
            names.Peek().Add( sName, sUniqueName );
        }

        public void AddFunctionCall( string sName ) {
            AddOp( OpType.Call, sName );
        }

        public void AddAssignment( string sName ) {
            AddOp( OpType.Assignment, sName );
        }

        public void AddLabel( string sLabel ) {
            AddOp( OpType.Label, sLabel );
        }

        public string GenerateLabel() {
            return "$label$" + labels.Count;
        }

        public void AddJumpStatement( string sLabel ) {
            AddOp( OpType.Jump, sLabel );
        }

        public void AddJumpIfNotStatement( string sLabel ) {
            AddOp( OpType.JumpIfNot, sLabel );
        }

        public void AddReturnStatement() {
            AddOp( OpType.Return );
        }

        public void AddPushCopy( string sLabel ) {
            AddOp( OpType.Copy, sLabel );
        }

        public void AddComment( string s ) {
            AddOp( OpType.Comment, s );
        }
        #endregion

        #region private function
        private void AddOp( OpType op, string s ) {
            ops.Add( new CatOp( op, s ) );
        }

        private void AddOp( OpType op ) {
            AddOp( op, "" );
        }
        #endregion

        #region IEnumerable<IOperation> Members

        public IEnumerator<IOperation> GetEnumerator() {
            foreach (CatOp op in ops)
                yield return op;
        }

        #endregion

        #region IEnumerable Members

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() {
            foreach (CatOp op in ops)
                yield return op;
        }

        #endregion

        public void WriteToStrings( List<String> strings ) {
            strings.Add( "define " + name );
            strings.Add( "  define return { pop }" );
            // TODO: add a define for each label.
            foreach (string s in labels)
                strings.Add( "  define " + s );
            strings.Add( "{" );
            strings.Add( "nil // the list containing the arguments" );
            foreach (KeyValuePair<string, string> kvp in args)
                strings.Add( "swons" );

            // step 2: define the "return" 
            strings.Add( "}" );
        }
    }

}
