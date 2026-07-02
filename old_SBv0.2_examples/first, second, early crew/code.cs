using System;
using System.Collections.Generic;
using System.Linq;

public class CPHInline
{
	public bool Execute()
	{
		// get OBS scene where we might want this information displayed
		string obsScene = args["EarlyCrewOBSScene"].ToString();
		// get OBS scene
		string whichScene = CPH.ObsGetCurrentScene();
		// if we're not on our starting soon scene, return
		if (whichScene != obsScene) {
			// TODO CPH.TwitchRedemptionCancel();
			return true;
		}
		
		// get user and reward ifno
		string userName = args["userName"].ToString();
		string redemptionId = args["redemptionId"].ToString();
		string rewardId = args["rewardId"].ToString();
		string rewardName = args["rewardName"].ToString();
		
		int crewSize = Int32.Parse(args["EarlyCrewSizeLimit"].ToString());
		if (crewSize < 1) {
			CPH.SendMessage("Sorry, must set EarlyCrewSizeLimit to a value greater than 1");
		}
		
		// get OBS source names
		string obsFirstSource = args["FirstOBSSource"].ToString();
		string obsSecondSource = args["SecondOBSSource"].ToString();
		string obsEarlyCrewSource = args["EarlyCrewOBSSource"].ToString();

		// get prefixes and early crew separator, if any
		string firstUsernamePrefix = args["FirstUsernamePrefix"].ToString() ?? "";
		string secondUsernamePrefix = args["SecondUsernamePrefix"].ToString() ?? "";
		string earlyCrewPrefix = args["EarlyCrewPrefix"].ToString() ?? "";
		string earlyCrewSeparator = args["EarlyCrewSeparator"].ToString() ?? "\n";
		if (earlyCrewSeparator == "\\n") {
			earlyCrewSeparator = "\n" ;
		}

		// get channel point redeem names so we know which channel redeem was called
		string cprFirst = args["FirstChannelPointRedeemName"].ToString();
		string cprSecond = args["SecondChannelPointRedeemName"].ToString();
		string cprEarlyCrew = args["EarlyCrewChannelPointRedeemName"].ToString();

		// fetch global variables
		String firstRedeem = CPH.GetGlobalVar<string>("736FirstRedeemUsername", true);
		String secondRedeem = CPH.GetGlobalVar<string>("736SecondRedeemUsername", true);
		List<String> earlyCrew = new List<String>(CPH.GetGlobalVar<string[]>("736EarlyCrewList", true) ?? new string[0]);
		string tmp = String.Join(",", earlyCrew);

		string obsSourceTarget = "";
		string obsOutput = "";

		// FIRST
		if (rewardName == cprFirst) {
			// check if user is second or early crew
			if (userName == secondRedeem) {
				CPH.SendMessage($"Sorry {userName}, you already claimed Second");
				CPH.TwitchRedemptionCancel(rewardId, redemptionId);
				return false;
			}
			if (earlyCrew.Contains(userName) || userName == secondRedeem) {
				CPH.SendMessage($"Sorry {userName}, you're already part of the early crew");
				CPH.TwitchRedemptionCancel(rewardId, redemptionId);
				return false;
			}
			
			// set global
			CPH.SetGlobalVar("736FirstRedeemUsername", userName, true);
			obsOutput = $"{firstUsernamePrefix} {userName}";
			obsSourceTarget = obsFirstSource.Trim();
		}
		
		// SECOND
		if (rewardName == cprSecond) {
			// check if user is first or early crew
			if (userName == firstRedeem) {
				CPH.SendMessage($"Sorry {userName}, you already claimed First");
				CPH.TwitchRedemptionCancel(rewardId, redemptionId);
				return false;
			}
			if (earlyCrew.Contains(userName)) {
				CPH.SendMessage($"Sorry {userName}, you're already part of the early crew");
				CPH.TwitchRedemptionCancel(rewardId, redemptionId);
				return false;
			}

			// set global
			CPH.SetGlobalVar("736SecondRedeemUsername", userName, true);
			obsOutput = $"{secondUsernamePrefix} {userName}";
			obsSourceTarget = obsSecondSource.Trim();
		}
		
		// EARLY CREW
		if (rewardName == cprEarlyCrew) {
			// check if user is first or second or early crew
			if (userName == firstRedeem) {
				CPH.SendMessage($"Sorry {userName}, you already claimed First");
				CPH.TwitchRedemptionCancel(rewardId, redemptionId);
				return false;
			}
			if (userName == secondRedeem) {
				CPH.SendMessage($"Sorry {userName}, you already claimed Second");
				CPH.TwitchRedemptionCancel(rewardId, redemptionId);
				return false;
			}
			if (earlyCrew.Contains(userName)) {
				CPH.SendMessage($"Sorry {userName}, you're already part of the early crew");
				CPH.TwitchRedemptionCancel(rewardId, redemptionId);
				return false;
			}

			// if list size >= EarlyCrewSizeLimit
			//CPH.SendMessage($"{earlyCrew.Count} / {crewSize}");
			if (earlyCrew.Count >= crewSize) {
				CPH.SendMessage($"Sorry {userName}, the crew is full!");
				CPH.TwitchRedemptionCancel(rewardId, redemptionId);
				return false;
			}

			// add user to the list
			earlyCrew.Add(userName);
			CPH.SetGlobalVar("736EarlyCrewList", earlyCrew.ToArray(), true);

			// build text for OBS
			if (earlyCrewPrefix != "") {
				obsOutput = $"{earlyCrewPrefix}\n";
			}
			obsOutput = String.Join(earlyCrewSeparator, earlyCrew);
			
			obsSourceTarget = obsEarlyCrewSource.Trim();
		}

		//CPH.SendMessage(obsOutput);
		CPH.LogInfo(obsOutput);
		CPH.ObsSetGdiText(obsScene, obsSourceTarget, obsOutput);
		
		return true;
	}
}
