using System;

public class MockStats {
	public static int getStat(string name) {
		if (name.Equals ("incisors")) {
			return 5;
		}
		else if (name.Equals("fangs")) {
			return 8;
		}
		return 2;
	}
}