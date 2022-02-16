using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WatchedProperty : System.Attribute
{
    public string TextColor;
    public string BackgroundColor;
    public float WarningRangeStart;
    public float WarningRangeEnd;
}

public class ExecutableAction : System.Attribute
{
    public string ActionName;
}

public class WatchedScript : System.Attribute
{
    public bool ShowPosition = true;
}
