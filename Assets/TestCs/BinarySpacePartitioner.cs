using System.Collections.Generic;
using JetBrains.Annotations;
using Unity.VisualScripting;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.Tilemaps;

public class BinarySpacePartitioner
{
    
    private RoomNode rootNode;
    private int dungeonWidth;
    private int dungeonLength;

    public RoomNode returnRootNode()
    {
        return rootNode;
    }
    
    public BinarySpacePartitioner( float roomWidthMin, float roomLengthMin,float dungeonWidth, float dungeonLength)
    {
        this.rootNode = new RoomNode(null,new Vector2(-10,-5) ,new Vector2(10,5),0,1);

    }
     
    public List<RoomNode> PrepareNodesCollection(int maxIterations, float roomWidthMin, float roomLengthMin)
    {
        Queue<RoomNode> graph = new Queue<RoomNode>();
        List<RoomNode> listtoReturn = new List<RoomNode>();
        graph.Enqueue(this.rootNode);
        listtoReturn.Add(this.rootNode);
        int iteration = 0;
        while ( iteration < maxIterations && graph.Count > 0 )
        {
            iteration++;
            RoomNode currentNode = graph.Dequeue();
            if (currentNode.NodeWidth >= roomWidthMin * 2 || currentNode.NodeLength>= roomLengthMin * 2)
            {
                SplitTheSpace(currentNode, listtoReturn, roomLengthMin, roomWidthMin, graph);
            }
        }
    
        return listtoReturn;
    }

    private void SplitTheSpace(RoomNode currentNode,List<RoomNode> listtoReturn, float roomWidthMin, float roomLenghtMin,Queue<RoomNode> graph)
    {
        bool widthStatus = currentNode.NodeWidth > roomWidthMin * 2;
        bool lengthStatus = currentNode.NodeLength > roomLenghtMin * 2;

        Orientation orientation;
        
        if (widthStatus && lengthStatus)
        {
            orientation = (Orientation)(Random.Range(0, 2));
        }
        
        else if (widthStatus)
        {
            orientation = Orientation.Vertical;
        }
        else
        {
            orientation = Orientation.Horizontal;
        }

        
        makingDividingSpace(orientation,currentNode,listtoReturn,roomWidthMin,roomLenghtMin,graph);
        
    }
    
    void makingDividingSpace(Orientation orientation,RoomNode currentNode,List<RoomNode> listtoReturn, float roomWidthMin, float roomLengthMin,Queue<RoomNode> graph)
    {
        if (orientation == Orientation.Vertical)
        {
            VerticalLine verticalLine = new VerticalLine(currentNode.BottomLeftCorner, currentNode.TopRightCorner,
                roomWidthMin, roomLengthMin);
        
        
        
            RoomNode node1 = new RoomNode(currentNode, currentNode.BottomLeftCorner, verticalLine.TopPoint,currentNode.TreeLayerIndex+1,currentNode.treeNumber+1);
            RoomNode node2 = new  RoomNode(currentNode, verticalLine.BottomPoint, currentNode.TopRightCorner,currentNode.TreeLayerIndex+1,currentNode.treeNumber+1);
            
         
            AddNewNodeToCollection(listtoReturn, graph, node1);
            AddNewNodeToCollection(listtoReturn, graph, node2);

        }
        else if (orientation == Orientation.Horizontal)
        {
            HorizontalLine horizontalLine = new HorizontalLine(currentNode.BottomLeftCorner, currentNode.TopRightCorner,
                roomWidthMin, roomLengthMin);
        
        
        
            RoomNode node1 = new RoomNode(currentNode, horizontalLine.LeftPoint,currentNode.TopRightCorner ,currentNode.TreeLayerIndex+1,currentNode.treeNumber+1);
            RoomNode node2 = new  RoomNode(currentNode,currentNode.BottomLeftCorner,horizontalLine.RightPoint,currentNode.TreeLayerIndex+1,currentNode.treeNumber+1);
            
            AddNewNodeToCollection(listtoReturn, graph, node1);
            AddNewNodeToCollection(listtoReturn, graph, node2);

        }
    }
    
    private void AddNewNodeToCollection(List<RoomNode> listtoReturn, Queue<RoomNode> graph, RoomNode node)
    {
        listtoReturn.Add(node);
        graph.Enqueue(node);
    }

}