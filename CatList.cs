using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Diagnostics;

namespace Cat
{    
    /// <summary>
    /// This is the base class for the primary Cat collection. There are 
    /// several different types of CatLists. The different kinds of lists, 
    /// improve performance.
    /// </summary>
    public abstract class CatList 
    {       
        private static EmptyList gNil = new EmptyList();

        // common constructors
        public static EmptyList nil() { return gNil; }
        public static UnitList unit(Object o) { return new UnitList(o); }
        public static ConsCell pair(Object second, Object first) { return new ConsCell(unit(second), first); }
        
        // These are static functions which remove the original argument, 
        // Notice that if I want to eventually use reference counting, I will 
        // have to modify these functions
        public static CatList cons(CatList x, Object o) { return x.append(o); }
        public static CatList cdr(CatList x) { return x.tail(); }        

        #region abstract functions        
        public abstract CatList dup();
        public abstract int count();
        public abstract Object nth(int n);
        public abstract CatList drop(int n);
        #endregion

        public static CatList map(CatList x, Function f)
        {
            return x.vmap(f);
        }

        public static Object foldl(CatList x, Object o, Function f)
        {
            return x.vfoldl(o, f);
        }

        public static CatList filter(CatList x, Function f)
        {
            return x.vfilter(f);
        }

        public virtual CatList vmap(Function f)
        {
            CatStack stk = new CatStack();
            for (int i = count(); i > 0; --i)
            {
                Object tmp = nth(i - 1);
                stk.Push(f.Invoke(tmp));
            }
            return new ListFromStack(stk);
        }
        public virtual Object vfoldl(Object x, Function f)
        {
            for (int i = 0; i < count(); ++i)
            {
                Object tmp = nth(i);
                x = f.Invoke(x, tmp);
            }
            return x;
        }
        /// <summary>
        /// TODO: This is a pretty awful implementation. In some cases O(n^2)
        /// </summary>
        public virtual CatList vfilter(Function f)
        {
            CatStack stk = new CatStack();
            for (int i = count(); i > 0; --i)
            {
                Object tmp = nth(i - 1);
                if ((bool)f.Invoke(tmp))
                    stk.Push(tmp);
            }
            return new ListFromStack(stk);
        }
        public virtual CatList append(Object o) 
        { 
            return new ConsCell(this, o); 
        }
        public virtual Object head()
        {
            return nth(count() - 1);
        }

        public virtual CatList tail()
        {
            return drop(1);
        }

        public string str()
        {
            return ToString();
        }

        public override string ToString()
        {
            string result = "( ";
            int nMax = count();
            if (nMax > 4) nMax = 4;

            for (int i = 0; i < nMax - 1; ++i)
            {
                result += nth(count() - (i + 1)).ToString();
                result += ", ";
            }

            if (nMax < count())
            {
                result += "..., ";
            }

            if (count() > 0)
                result += nth(0).ToString();
            result += ")";
            return result;
        }
    }

    /// <summary>
    /// An EmptyList is a special case of a CatList with no items
    /// </summary>
    public class EmptyList : CatList
    {
        #region public functions
        public override CatList append(Object o) 
        { 
            return unit(o); 
        }
        public override CatList dup() 
        { 
            return this; 
        }
        public override int count() 
        { 
            return 0; 
        }
        public override CatList vmap(Function f) 
        { 
            return this; 
        }
        public override Object vfoldl(Object o, Function f) 
        { 
            return o; 
        }
        public override CatList vfilter(Function f)
        {
            return this;
        }
        public override Object nth(int n) 
        { 
            throw new Exception("no items"); 
        }
        public override CatList drop(int n) 
        { 
            if (n != 0) 
                throw new Exception("no items"); 
            return this; 
        }
        public override object head()
        {
            throw new Exception("empty list, no head");
        }
        public override CatList tail()
        {
            throw new Exception("empty list, no tail");
        }
        #endregion
    }

    /// <summary>
    /// A UnitList is a special case of a CatList with one item
    /// </summary>
    public class UnitList : CatList
    {       
        private Object m;
        public UnitList(Object o) { m = o; } 

        #region public functions
        public override CatList dup() 
        { 
            return unit(m); 
        }
        public override int count() 
        { 
            return 1; 
        }
        public override CatList vmap(Function f) 
        { 
            return unit(f.Invoke(m)); 
        }
        public override Object vfoldl(Object o, Function f) 
        { 
            return f.Invoke(o, m); 
        }
        public override CatList vfilter(Function f)
        {
            if ((bool)f.Invoke(m))
                return unit(m);
            else
                return nil();
        }
        public override Object nth(int n) 
        { 
            if (n != 0) 
                throw new Exception("only one item in list"); 
            return m; 
        }
        public override CatList drop(int n) 
        {
            switch (n)
            {
                case 0: return this;
                case 1: return nil();
                default: throw new Exception("list only has one item");
            }
        }
        public override object head()
        {
            return m;
        }
        public override CatList tail()
        {
            return nil();
        }
        #endregion
    }

    /// <summary>
    /// A ConsCell is a very naive implementation of a functional list
    /// </summary>
    public class ConsCell : CatList
    {
        Object mHead;
        CatList mTail;
        public ConsCell(CatList list, Object o)
        {
            mHead = o;
            mTail = list;
        }
        public override CatList dup() 
        { 
            return this; 
        }
        public override int count() 
        { 
            return 1 + mTail.count(); 
        }
        public override CatList vmap(Function f)
        {
            return cons(mTail.vmap(f), f.Invoke(mHead));
        }
        public override Object vfoldl(Object o, Function f)
        {
            o = f.Invoke(o, mHead);
            return mTail.vfoldl(o, f);
        }
        public override CatList vfilter(Function f)
        {
            if ((bool)f.Invoke(mHead))
                return cons(mTail.vfilter(f), mHead);
            else
                return mTail.vfilter(f);
        }
        public override Object nth(int n) 
        { 
            if (n == 0) 
                return mHead; 
            else 
                return mTail.nth(n - 1); 
        }
        public override CatList drop(int n) 
        { 
            if (n == 0) 
                return this; 
            else 
                return mTail.drop(n - 1); 
        }
        public override object head()
        {
            return mHead;
        }
        public override CatList tail()
        {
            return mTail;
        }
    }

    /// <summary>
    /// Wraps a CatStack in a CatList
    /// </summary>
    public class ListFromStack : CatList
    {
        CatStack mStk;
        public ListFromStack(CatStack stk)
        {
            mStk = stk;
        }
        public override CatList dup() 
        { 
            return this; 
        }
        public override int count() 
        { 
            return mStk.Count; 
        }
        public override Object nth(int n) 
        { 
            return mStk[n]; 
        }
        public override CatList drop(int n) 
        {
            if (n == 0) 
                return this;
            
            switch (count() - n)
            {
                case 0:
                    return nil();
                case 1:
                    return unit(mStk[count() - 1]);
                default:
                    return new SubList(this, count() - n);
            }
        }
    }

    /// <summary>
    /// A SubList is a view into a ListFromStack. It is like a generalization
    /// of a ConsCell
    /// </summary>
    public class SubList : CatList
    {
        CatList mList;
        int mCount;
        int mOffset;
        public SubList(CatList list, int cnt)
        {
            Trace.Assert(cnt >= 2);
            Trace.Assert(cnt < list.count());
            mList = list;
            mCount = cnt;
            mOffset = list.count() - cnt;
        }
        public override CatList dup() 
        { 
            return this; 
        }
        public override int count() 
        { 
            return mCount; 
        }
        public override Object nth(int n) 
        {
            Trace.Assert(mOffset + mCount == mList.count());
            if (n >= mCount) 
                throw new Exception("index out of range"); 
            return mList.nth(n + mOffset); 
        }
        public override CatList drop(int n)
        {
            if (n == 0)
                return this;

            switch (count() - n)
            {
                case 0:
                    return nil();
                case 1:
                    return unit(nth(mCount - 1));
                default:
                    return new SubList(mList, mCount - n);
            }
        }
    }
    
    /// <summary>
    /// Also known as a generator, a lazy list generates values as they are requested
    /// Some operations (such as "drop" and "dup" and "map) are always very fast, whereas count
    /// or nth will be O(n) complexity. A LazyList can be used to create infinite lists. For example all 
    /// positive even numbers can be expressed as "0 [true] [2 +] []" 
    /// </summary>
    public class LazyList : CatList 
    {
        Object mInit;
        Function mNext;
        Function mCond;
        Function mMapF;

        private LazyList(Object init, Function cond, Function next, Function mapf)
        {
            mInit = init;
            mNext = next;
            mCond = cond;
            mMapF = mapf;
        }

        public LazyList(Object init, Function cond, Function next)
        {
            mInit = init;
            mNext = next;
            mCond = cond;
            mMapF = null;
        }

        public override CatList dup()
        {
            return new LazyList(mInit, mCond, mNext, mMapF);
        }

        public override int count()
        {
            int n = 0;
            Object o = mInit;
            while ((bool)mCond.Invoke(o))
            {
                ++n;
                o = mNext.Invoke(o);
            }
            return n;           
        }

        private Object nomap_nth(int n)
        {
            Object o = mInit;
            while ((bool)mCond.Invoke(o))
            {
                if (n-- == 0)
                    return o;

                o = mNext.Invoke(o);
            }
            throw new Exception("out of bounds");
        }

        public override Object nth(int n)
        {
            Object o = nomap_nth(n);
            if (mMapF != null)
                return mMapF.Invoke(o);
            else
                return o;
        }
        public override CatList drop(int n)
        {
            return new LazyList(nomap_nth(n), mNext, mCond, mMapF);
        }
        public override Object head()
        {
            if (mMapF != null)
                return mMapF.Invoke(mInit);
            else
                return mInit;
        }
        public override CatList vmap(Function f)
        {
            if (mMapF == null)
                return new LazyList(mInit, mCond, mNext, f);
            else
                return new LazyList(mInit, mCond, mNext, new ComposedFunction(mMapF, f));
        }
        public override Object vfoldl(Object x, Function f)
        {
            Object cur = mInit;
            Object result = x;
            while ((bool)mCond.Invoke(cur))
            {
                if (mMapF != null)
                    result = f.Invoke(result, mMapF.Invoke(cur));
                else
                    result = f.Invoke(result, cur);
                cur = mNext.Invoke(cur);
            }
            return result ;
        }
        public override string ToString()
        {
            string result = "(";
            if ((bool)mCond.Invoke(mInit))
            {
                result += nth(0).ToString();
                Object next = mNext.Invoke(mInit);
                if ((bool)mCond.Invoke(next))
                {
                    result += ", " + nth(1).ToString() + " ..";
                }
            }
            result += ")";
            return result;
        }       
    }    
}
