using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Unity.VisualScripting;
using UnityEngine;
using Random = UnityEngine.Random;


public class HorizontalLine:Line
{
    
    private float widthMin;
    private float heightMin;
    public List <Vector2> TwoPoint;
    public float yCoordinate;
    private Unity.Mathematics.Random random = new Unity.Mathematics.Random();
    
    private Vector2 leftPoint;
    private Vector2 rightPoint;

    public Vector2 LeftPoint
    {
        get => leftPoint;
        set => leftPoint = value;
    }

    public Vector2 RightPoint
    {
        get => rightPoint;
        set => rightPoint = value;
    }
    
    public HorizontalLine(Vector2 BottomLeftCorner,Vector2 TopRightCorner,float widthMin, float heightMin)
    {
        this.heightMin = heightMin;
        this.widthMin = widthMin;
        
        yCoordinate = Random.Range((BottomLeftCorner.y + heightMin), (TopRightCorner.y - heightMin));
        this.LeftPoint = new Vector2(BottomLeftCorner.x, yCoordinate);
        this.RightPoint = new Vector2(TopRightCorner.x, yCoordinate);

    }
    
 
}