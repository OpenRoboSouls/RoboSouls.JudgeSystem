using System;

namespace RoboSouls.JudgeSystem.Attributes;

[Flags]
public enum PropertyStorageMode
{
    Single,
    Identity,
    Camp
}

[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class Property(string storageProvider, 
    PropertyStorageMode mode = PropertyStorageMode.Single | PropertyStorageMode.Identity | PropertyStorageMode.Camp,
    string? id = null) : Attribute
{
    
}