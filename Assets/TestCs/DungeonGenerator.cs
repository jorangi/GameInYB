using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TreeEditor;
using UnityEngine;
using Node = UnityEditor.Experimental.GraphView.Node;



public class DungeonCreator : MonoBehaviour
{
    public GameObject tile;

    public float  dungeonWidth;
    public float  dungeonLength;
    public float roomWidthMin;
    public float roomLengthMin;
    public int maxIterations;
    
    
    private List<RoomNode> _list = new List<RoomNode>();
    private List<Vector2> vectorList = new List<Vector2>();
    

    void Start()
    {
        
        
        BinarySpacePartitioner binarySpacePartitioner = new BinarySpacePartitioner(roomWidthMin,roomLengthMin,dungeonWidth,dungeonLength);

        _list = binarySpacePartitioner.PrepareNodesCollection( maxIterations,  roomWidthMin,  roomLengthMin);


        List<Vector3> lineList = new List<Vector3>();


        RoomNode rootNode = binarySpacePartitioner.returnRootNode();

        global::Node currentNode = rootNode;
        Queue<global::Node> roomNodeQueue = new Queue<global::Node>();
        roomNodeQueue.Enqueue(rootNode);
        while (roomNodeQueue.Count != 0 )
        {
            currentNode = roomNodeQueue.Dequeue();
            Debug.Log(currentNode.treeNumber);
           
            if (currentNode.ChildrenNodeList.Count != 0)
            {
                roomNodeQueue.Enqueue(currentNode.ChildrenNodeList[0]);
                roomNodeQueue.Enqueue(currentNode.ChildrenNodeList[1]);
            }

         
            
            
            
        }

        for (int i = 0; i < _list.Count; i++)
        {
            GameObject newTile = Instantiate(tile);
            newTile.transform.position =  new Vector3(_list[i].BottomLeftCorner.x,_list[i].BottomLeftCorner.y, 0);
            GameObject newTile1 = Instantiate(tile);
            newTile1.transform.position =  new Vector3(_list[i].BottomRightCorner.x,_list[i].BottomRightCorner.y, 0);
            GameObject newTile2 = Instantiate(tile);
            newTile2.transform.position =  new Vector3(_list[i].TopLeftCorner.x,_list[i].TopLeftCorner.y, 0);
            GameObject newTile3 = Instantiate(tile);
            newTile3.transform.position =  new Vector3(_list[i].TopRightCorner.x,_list[i].TopRightCorner.y, 0);
        }

        for (int i = 0; i < _list.Count; i++)
        {
            lineList.Add(new Vector3(_list[i].BottomLeftCorner.x, _list[i].BottomLeftCorner.y, 0));
            lineList.Add(new Vector3(_list[i].BottomRightCorner.x, _list[i].BottomRightCorner.y, 0));
            lineList.Add(new Vector3(_list[i].TopLeftCorner.x, _list[i].TopLeftCorner.y, 0));
            lineList.Add(new Vector3(_list[i].TopRightCorner.x, _list[i].TopRightCorner.y, 0));


        }

        lineList = lineList.Distinct().ToList();
        for (int i = 0; i < lineList.Count; i++)
        {
            for (int j = 0; j < lineList.Count; j++)
            {
                if (!((lineList[i].x != lineList[j].x) && (lineList[i].y != lineList[j].y)))
                {
                    Debug.DrawLine(lineList[i],lineList[j], Color.red, 1000.0f);

                }
            }
                
                
        }

    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
