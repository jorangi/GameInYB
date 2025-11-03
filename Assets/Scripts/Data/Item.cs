using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

[Serializable]
public struct ItemAttribute
{
    public ItemAttribute(string stat = "", string op = "", float value = 0f)
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
public struct Item
{
    public string id;
    public string[] name;
    public string rarity;
    public string[] description;
    public ItemAttribute[] attributes;
    public string[] skills;
    public bool twoHander;
    public bool stackable;
    private ItemProvide provider;
    //public readonly ItemProvide GetProvider() => provider ?? new ItemProvide(this);
    public ItemProvide GetProvider()
    {
        provider ??= new ItemProvide(this);
        return provider;
    }
    public static bool operator ==(Item i1, Item i2)
    {
        if (ReferenceEquals(i1, null))
            return ReferenceEquals(i2, null);
        return i1.id == i2.id;
    }
    public static bool operator !=(Item i1, Item i2) => !(i1 == i2);
    public override bool Equals(object obj)
    {
        if (obj is Item otherItem) return this.id == otherItem.id;
        return false;
    }
    public override int GetHashCode()
    {
        return id.GetHashCode();
    }
    public readonly override string ToString()
    {
        var attrStr = string.Join(", ", (attributes ?? Array.Empty<ItemAttribute>())
                                        .Select(a => a.ToString()));

        var nameStr = string.Join(", ", name ?? Array.Empty<string>());
        var descStr = string.Join(", ", description ?? Array.Empty<string>());
        var skillStr = string.Join(", ", skills ?? Array.Empty<string>());

        return $"id:{id}, name:[{nameStr}], rarity:{rarity}, description:[{descStr}], attributes:[{attrStr}], two_hander:{twoHander}, stackable:{stackable}, skills:[{skillStr}]";
    }
}
public sealed class ItemProvide : IStatModifierProvider
{
    private readonly Item item;
    private readonly string id;
    private readonly int attrsHash;
    public ItemProvide(in Item item)
    {
        this.item = item;
        this.id = item.id ?? "00000";

        int h = 17;
        h = h * 31 + id.GetHashCode();
        if (item.attributes != null)
        {
            for (int i = 0; i < item.attributes.Length; i++)
            {
                var a = item.attributes[i];
                h = h * 31 + (a.stat?.ToUpperInvariant().GetHashCode() ?? 0);
                h = h * 31 + (a.op?.ToUpperInvariant().GetHashCode() ?? 0);
                h = h * 31 + a.value.GetHashCode();
            }
        }
        attrsHash = h;
    }
    public IEnumerable<StatModifier> GetStatModifiers()
    {
        var a = item.attributes;
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
        if (obj is not ItemProvide other) return false;
        return id == other.id && attrsHash == other.attrsHash;
    }
    public override int GetHashCode() => HashCode.Combine(id, attrsHash);
}
public class ItemBuilder
{
    private string id = "00000";
    private string[] name = new string[2];
    private string rarity = "common";
    private string[] description = new string[2];
    private ItemAttribute[] attributes;
    private string[] skills;
    private bool two_hander = false;
    private bool stackable = false;
    public ItemBuilder SetId(string id)
    {
        this.id = id;
        return this;
    }
    public ItemBuilder SetName(string[] name)
    {
        this.name = name;
        return this;
    }
    public ItemBuilder SetRarity(string rarity)
    {
        this.rarity = rarity;
        return this;
    }
    public ItemBuilder SetDescription(string[] description)
    {
        this.description = description;
        return this;
    }
    public ItemBuilder SetAttributes(ItemAttribute[] attributes)
    {
        this.attributes = attributes;
        return this;
    }
    public ItemBuilder SetSkills(string[] skills)
    {
        this.skills = skills;
        return this;
    }
    public ItemBuilder SetTwoHander(bool two_hander)
    {
        this.two_hander = two_hander;
        return this;
    }
    public ItemBuilder SetStackable(bool stackable)
    {
        this.stackable = stackable;
        return this;
    }
    public Item Build()
    {
        return new()
        {
            id = this.id,
            name = this.name,
            rarity = this.rarity,
            description = this.description,
            attributes = this.attributes,
            skills = this.skills,
            twoHander = this.two_hander,
            stackable = this.stackable
        };
    }
}
