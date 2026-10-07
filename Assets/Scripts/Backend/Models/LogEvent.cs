using System;

[Serializable]
public class LogEvent
{
    public string timestamp;
    public string source;
    public string category;
    public string message;
}