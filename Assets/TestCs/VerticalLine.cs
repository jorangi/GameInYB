using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VerticalLine:Line
{
    
    private float widthMin;
    private float heightMin;
    public List <Vector2> TwoPoint;
    public float xCoordinate;

    private Vector2 topPoint;
    private Vector2 bottomPoint;

    public Vector2 TopPoint
    {
        get => topPoint;
        set => topPoint = value;
    }

    public Vector2 BottomPoint
    {
        get => bottomPoint;
        set => bottomPoint = value;
    }
   
    
    private Unity.Mathematics.Random random = new Unity.Mathematics.Random();
    
    
    public VerticalLine(Vector2 BottomLeftCorner,Vector2 TopRightCorner,float widthMin, float heightMin)
    {
       
        this.heightMin = heightMin;
        this.widthMin = widthMin;
        
        xCoordinate = Random.Range((BottomLeftCorner.x + widthMin), (TopRightCorner.x - widthMin));
        this.TopPoint = new Vector2(xCoordinate,TopRightCorner.y);
        this.BottomPoint = new Vector2(xCoordinate, BottomLeftCorner.y);
    }
    
    
    

   
}