using System.Collections;
using System.Collections.Generic;
using UnityEngine;

abstract public class Node
{
    private List<Node> childreNodeList;
    
    public List<Node> ChildrenNodeList
    {
        get => childreNodeList;
    }
    
    public bool Visted { get; set; }

    public Vector2  TopRightCorner { get; set; }
    
    public  Vector2  TopLeftCorner { get; set; }
    
    public  Vector2  BottomLeftCorner{ get; set; }
    
    public  Vector2  BottomRightCorner { get; set; }
    
    public float NodeWidth { get; set; }
    
    public float NodeLength { get; set; }
    public Node Parent { get; set; }
    
    public int TreeLayerIndex { get; set; }
    
    public int treeNumber { get; set; }
    public Node(Node parentNode)
    {
        childreNodeList = new List<Node>();
        this.Parent = parentNode;
        if (parentNode != null)
        {
            parentNode.AddChild(this);
        }
    }

    public void AddChild(Node node)
    {
        childreNodeList.Add(node);
    }

    public void RemoveChild(Node node)
    {
        childreNodeList.Remove(node);
    }



}
