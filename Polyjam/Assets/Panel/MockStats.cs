using System;

public class MockStats {
	public static int fangs = 0;
	public static int incisors = 0;
	public static int hands = 0;
	public static int legs = 0;
	public static int brain = 0;
	public static int eyes = 0;
	public static int nose = 0;
	public static int liver = 0;


	static int[] fangCost = new int[10] {22, 24, 30, 40, 70, 130, 260, 560, 1200, 2600};
	static int[] incisorsCost = new int[10] {22, 24, 30, 40, 70, 130, 260, 560, 1200, 2600};
	static int[] handsCost = new int[10] {32, 36, 45, 60, 120, 270, 640, 1500, 3800, 9500};
	static int[] legsCost = new int[10] {32, 36, 45, 60, 120, 270, 640, 1500, 3800, 9500};
	static int[] brainCost = new int[10] {52, 57, 69, 100, 190, 430, 1000, 2800, 7600, 20000};
	static int[] eyesCost = new int[10] {42, 46, 57, 80, 150, 340, 840, 2100, 5400, 14000};
	static int[] noseCost = new int[10] {42, 46, 57, 80, 150, 340, 840, 2100, 5400, 14000};
	static int[] liverCost = new int[10] {27, 30, 37, 50, 80, 170, 360, 800, 1800, 4100};

	public static int getStat(string name) {
		if (name.Equals ("incisors")) {
			return incisors;
		} else if (name.Equals ("fangs")) {
			return fangs;
		} else if (name.Equals ("hands")) {
			return hands;
		} else if (name.Equals ("legs")) {
			return legs;
		} else if (name.Equals ("brain")) {
			return brain;
		} else if (name.Equals ("eyes")) {
			return eyes;
		} else if (name.Equals ("nose")) {
			return nose;
		} else if (name.Equals ("liver")) {
			return liver;
		}
		return 0;
	}

	public static int getCost(string name) {
		if (name.Equals ("incisors")) {
			return incisorsCost[incisors];
		}
		else if (name.Equals("fangs")) {
			return fangCost[fangs];
		}
		else if (name.Equals("hands")) {
			return handsCost[hands];
		}
		else if (name.Equals("legs")) {
			return legsCost[legs];
		}
		else if (name.Equals("brain")) {
			return brainCost[brain];
		}
		else if (name.Equals("eyes")) {
			return eyesCost[eyes];
		}
		else if (name.Equals("nose")) {
			return noseCost[nose];
		}
		else if (name.Equals("liver")) {
			return liverCost[liver];
		}
		return 0;
	}
}