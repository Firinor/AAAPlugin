using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestSerializable : MonoBehaviour
{
    //[SerializeReference] 
    private List<Ability> list;
}

[Serializable]
public class Ability{}
public class S : Ability
{
    public int a;
}

public class B : Ability
{
    public string g;
}
