using UnityEngine;
using System.Collections.Generic;
using Newtonsoft.Json;

public class CellState
{
    public string alert;         
    public List<List<int>> door; 
    public bool down;            
    public bool entrance;        
    public string fire;          
    
    [JsonProperty("is agent")]
    public string is_agent;      
    
    public bool left;            
    public List<int> pos;        
    public bool right;           
    public string step;          
    public bool up;              
}