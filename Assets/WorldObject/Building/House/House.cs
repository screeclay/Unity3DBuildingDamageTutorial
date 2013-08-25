using UnityEngine;
using System.Collections;

public class House : Building {

	protected override void Start () {
		base.Start();
		//actions = new string[] {"Harvester"};
	}
	
	/*public override void PerformAction(string actionToPerform) {
		//base.PerformAction(actionToPerform);
		//CreateUnit(actionToPerform);
	}*/
}