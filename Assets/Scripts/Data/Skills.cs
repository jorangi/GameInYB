using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
public struct SkillAttribute
{
    public SkillAttribute(string stat = "", string op = "", float value = 0f)
    {
        this.stat = stat;
        this.op = op;
        this.value = value;
    }
    public string stat;
    public string op;
    public float value;
    public override readonly string ToString()
    {
        return $"stat:{stat}, op:{op}, value:{value}";
    }
}
[Serializable]
public struct Skill
{
    public string id;
    public string[] name;
    public string[] description;
    public SkillAttribute[] attributes;
    private SkillProvide provider;
    //public readonly SkillProvide GetProvider() => provider ?? new SkillProvide(this);
    public SkillProvide GetProvider()
    {
        provider ??= new SkillProvide(this);
        return provider;
    }
    public static bool operator ==(Skill i1, Skill i2)
    {
        if (ReferenceEquals(i1, null))
            return ReferenceEquals(i2, null);
        return i1.id == i2.id;
    }
    public static bool operator !=(Skill i1, Skill i2) => !(i1 == i2);
    public override bool Equals(object obj)
    {
        if (obj is Skill otherSkill) return this.id == otherSkill.id;
        return false;
    }
    public override int GetHashCode()
    {
        return id.GetHashCode();
    }
    public readonly override string ToString()
    {
        var attrStr = string.Join(", ", (attributes ?? Array.Empty<SkillAttribute>())
                                        .Select(a => a.ToString()));

        var nameStr = string.Join(", ", name ?? Array.Empty<string>());
        var descStr = string.Join(", ", description ?? Array.Empty<string>());

        return $"id:{id}, name:[{nameStr}], description:[{descStr}], attributes:[{attrStr}]";
    }
}
public sealed class SkillProvide : IStatModifierProvider
{
    private readonly Skill skill;
    private readonly string id;
    private readonly int attrsHash;
    public SkillProvide(in Skill skill)
    {
        this.skill = skill;
        this.id = skill.id ?? "00000";

        int h = 17;
        h = h * 31 + id.GetHashCode();
        if (skill.attributes != null)
        {
            for (int i = 0; i < skill.attributes.Length; i++)
            {
                var a = skill.attributes[i];
                h = h * 31 + (a.stat?.ToUpperInvariant().GetHashCode() ?? 0);
                h = h * 31 + (a.op?.ToUpperInvariant().GetHashCode() ?? 0);
                h = h * 31 + a.value.GetHashCode();
            }
        }
        attrsHash = h;
    }
    public IEnumerable<StatModifier> GetStatModifiers()
    {
        var a = skill.attributes;
        if (a is null) yield break;
        for (int i = 0; i < a.Length; i++)
        {
            var attr = a[i];
            yield return
            new(
                Enum.Parse<StatType>(attr.stat.ToUpperInvariant()),
                attr.value,
                Enum.Parse<StatOp>(attr.op.ToUpperInvariant()),
                this
            );
        }
    }
    public override bool Equals(object obj)
    {
        if (ReferenceEquals(this, obj)) return true;
        if (obj is not SkillProvide other) return false;
        return id == other.id && attrsHash == other.attrsHash;
    }
    public override int GetHashCode() => HashCode.Combine(id, attrsHash);
}
public class SkillBuilder
{
    private string id = "00000";
    private string[] name = new string[2];
    private string[] description = new string[2];
    private SkillAttribute[] attributes;
    public SkillBuilder SetId(string id)
    {
        this.id = id;
        return this;
    }
    public SkillBuilder SetName(string[] name)
    {
        this.name = name;
        return this;
    }
    public SkillBuilder SetDescription(string[] description)
    {
        this.description = description;
        return this;
    }
    public SkillBuilder SetAttributes(SkillAttribute[] attributes)
    {
        this.attributes = attributes;
        return this;
    }
    public Skill Build()
    {
        return new()
        {
            id = this.id,
            name = this.name,
            description = this.description,
            attributes = this.attributes,
        };
    }
}
