using System;

namespace RoboSouls.JudgeSystem.Attributes;

[AttributeUsage(AttributeTargets.Property)]
public class Hashed(string value = "") : Attribute;