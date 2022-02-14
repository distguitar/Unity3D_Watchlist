using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WatchedProperty : System.Attribute
{
    public float R = -1;
    public float G = -1;
    public float B = -1;
    public float A = -1;
}

public class ExecutableAction : System.Attribute
{
    public string ActionName;
}

public class WatchedScript : System.Attribute
{
    public bool ShowPosition = true;
}
