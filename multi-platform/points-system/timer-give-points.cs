using System;
using id736 = iandouglas736;

public class CPHInline
{
	public bool Execute()
	{
		if (!CPH.ObsIsStreaming())
			return false;

		string platform = "twitch";
		CPH.TryGetArg("platform", out platform);

		int pointsToAdd = 10;
		CPH.TryGetArg("pointsToAdd", out pointsToAdd);

		CPH.SetArgument("whoDunnit", "id736bot");
		CPH.SetArgument("recipient", "everyone");
		CPH.SetArgument("pointsToAdd", pointsToAdd);
		CPH.SetArgument("platform", platform);
		CPH.RunAction("give points logic", true);

		return true;
	}
}
