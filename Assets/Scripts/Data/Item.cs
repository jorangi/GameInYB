using System;
using UnityEngine;

[Serializable]
public struct ItemAttribute
{
    public float hp;
    public float atk;
    public float ats;
    public float def;
    public float cri;
    public float crid;
    public float spd;
    public float jmp;
    public int jCnt;
    public bool two_hander;
    public bool stackable;
    public override readonly string ToString()
    {
        return $"hp:{hp}, atk:{atk}, ats:{ats}, def:{def}, cri:{cri}, crid:{crid}, spd:{spd}, jmp:{jmp}, jCnt:{jCnt}, two_hander:{two_hander}, stackable:{stackable}";
    }
}

public class ItemAttributeBuilder
{
    private float hp = 0f;
    private float atk = 0f;
    private float ats = 0f;
    private float def = 0f;
    private float cri = 0f;
    private float crid = 0f;
    private float spd = 0f;
    private float jmp = 0f;
    private int jCnt = 0;
    private bool two_hander = false;
    private bool stackable = false;
    public ItemAttributeBuilder SetHP(float hp)
    {
        this.hp = hp;
        return this;
    }
    public ItemAttributeBuilder SetAtk(float atk)
    {
        this.atk = atk;
        return this;
    }
    public ItemAttributeBuilder SetAts(float ats)
    {
        this.ats = ats;
        return this;
    }
    public ItemAttributeBuilder SetDef(float def)
    {
        this.def = def;
        return this;
    }
    public ItemAttributeBuilder SetCri(float cri)
    {
        this.cri = cri;
        return this;
    }
    public ItemAttributeBuilder SetCrid(float crid)
    {
        this.crid = crid;
        return this;
    }
    public ItemAttributeBuilder SetSpd(float spd)
    {
        this.spd = spd;
        return this;
    }
    public ItemAttributeBuilder SetJmp(float jmp)
    {
        this.jmp = jmp;
        return this;
    }
    public ItemAttributeBuilder SetJCnt(int jCnt)
    {
        this.jCnt = jCnt;
        return this;
    }
    public ItemAttributeBuilder SetTwoHander(bool two_hander)
    {
        this.two_hander = two_hander;
        return this;
    }
    public ItemAttributeBuilder SetStackable(bool stackable)
    {
        this.stackable = stackable;
        return this;
    }
    public ItemAttribute Build()
    {
        return new()
        {
            hp = this.hp,
            atk = this.atk,
            ats = this.ats,
            def = this.def,
            cri = this.cri,
            crid = this.crid,
            spd = this.spd,
            jmp = this.jmp,
            jCnt = this.jCnt,
            two_hander = this.two_hander,
            stackable = this.stackable
        };
    }
}

[Serializable]
public struct Item
{
    public string id;
    public string[] name;
    public string rarity;
    public string[] description;
    public ItemAttribute attributes;
    public string[] skills;
    public override readonly string ToString()
    {
        return $"id:{id}, name:[{name.EnToString(", ")}], rarity:{rarity}, description:[{description.EnToString(", ")}], attributes:[{attributes}], skills:[{skills.EnToString(", ")}]";
    }
}
public class ItemBuilder
{
    private string id = "00000";
    private string[] name = new string[2];
    private string rarity = "common";
    private string[] description = new string[2];
    private ItemAttribute attributes = new ItemAttributeBuilder().Build();
    private string[] skills;
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
    public ItemBuilder SetAttributes(ItemAttribute attributes)
    {
        this.attributes = attributes;
        return this;
    }
    public ItemBuilder SetSkills(string[] skills)
    {
        this.skills = skills;
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
            skills = this.skills
        };
    }
}