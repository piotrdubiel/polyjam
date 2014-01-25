using System;

public class MockStats {
	public static int fangs = 0;
	public static int getStat(string name) {
		if (name.Equals ("incisors")) {
			return 5;
		}
		else if (name.Equals("fangs")) {
			return fangs;
		}
		else if (name.Equals("brain")) {
			return 10;
		}
		return 2;
	}

	public static int getCost(string name) {
		if (name.Equals ("incisors")) {
			return 50;
		}
		else if (name.Equals("fangs")) {
			return 80;
		}
		return 100;
	}
}