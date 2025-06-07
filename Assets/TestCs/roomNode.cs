using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoomNode : Node
{
   
   public RoomNode(Node parentNode, Vector2 bottomLeftCorner,Vector2 topRightCorner,int treeLayerIndex,int treeNumber) : base(parentNode)
   {
       this.TopRightCorner = topRightCorner;
       this.TopLeftCorner = new Vector2(bottomLeftCorner.x, topRightCorner.y);
       this.BottomLeftCorner = bottomLeftCorner;
       this.BottomRightCorner = new Vector2(topRightCorner.x, bottomLeftCorner.y);
       
       this.NodeWidth = this.TopRightCorner.x - this.TopLeftCorner.x;
       this.NodeLength = this.TopRightCorner.y - this.BottomRightCorner.y;
       this.TreeLayerIndex = treeLayerIndex;

       this.treeNumber = treeNumber;

   }

  
   
}