using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
/*
public class NewBehaviourScript : MonoBehaviour
{
    [SerializeField] private A a;
    
    void Start()
    {
        Boo();
        
        void Boo(){}
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
*/
public class A1
{
    public class B2
    {
        
    }
    
    private class B3
    {
        
    }
}

public class C3<T>
{
    
}

public static class D4
{
    
}

namespace MyNamespace
{
    public class E5
    {
        
    }
}

public enum J6
{
    
}

public class H7
{
    public A1 pubA7;
    private A1 priA7;
    public A1 pubPropA7 { get; }
    private A1 priPropA7 { get; }
    protected A1 protec7;
    protected A1 protecProp7 { get; }
    protected virtual void BarrierH7(){}
}

public abstract class K8Child : H7
{
    public A1 pubA;
    private A1 priA;
    public A1 pubPropA { get; }
    private A1 priPropA{ get;}
    
    public K8Child(){}
    public K8Child(int a){}
    protected K8Child(A1 a){}
    private K8Child(string b) { }

    protected void Barrier()
    {
        
    }
    protected static void BarrierStatic()
    {
        
    }
    protected virtual void BarrierVirtual()
    {
        
    }

    protected override void BarrierH7()
    {
    }

    protected abstract void BarrierAbstract();
}

public interface L9{}

internal abstract class M10 : L9
{
    public void M10m()
    {
        
    }
}

public struct K11struct
{
        
}