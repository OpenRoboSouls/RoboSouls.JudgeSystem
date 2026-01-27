using System;

namespace RoboSouls.JudgeSystem.Attributes;

[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class Property(string storageProvider, bool withIdentity = false, bool withCamp = true): Attribute
{
    
}