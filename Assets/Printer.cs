using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Printer  {


    public static void Print(string name, int[] array, bool leaf)
    {
        string values = "";
        string problems = "";

        for (int i = 0; i < array.Length; i++)
        {
            if ((i != 0) && (array[i - 1] > array[i]))
                problems += "Discontinuity found at " + i + "!! \n";
            if (leaf)
                values += (int)array[i] / 10000000 + " ";
            else
                values += (int)array[i] / 1000 + " ";

            //if ((i != 0) && (array[i - 1].morton > array[i].morton))
            //    problems += "Discontinuity found at " + i + "!! \n";
            // values += "<" + array[i].sPos.ToString() + ":" + array[i].sRadius + "> " + "\n";// + "\n" + "<" + array[i].intNodes.x + ":" + array[i].intNodes.y + ">__" + "\n";
            //if (leaf)
            //values += array[i].parentId + ", "+ array[i].objectId + " , Morton ->" + array[i].mortonId +"\n";
            //else
            //values += array[i].parentId + " Leaves>(" + array[i].leaves.x + "," + array[i].leaves.y + ")  " + " Nodes>(" + array[i].intNodes.x + ":" + array[i].intNodes.y + ") Morton ->" + array[i].mortonId + "\n";

        }

        Debug.Log(name + " : \n" + values + "\n" + problems);
    }

}
