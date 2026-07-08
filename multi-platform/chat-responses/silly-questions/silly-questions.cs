using System;
using System.Collections.Generic;
using id736 = iandouglas736;

public class CPHInline
{
	private static readonly Random _random = new Random();

	private static readonly List<string> _questions = new List<string> {
		"Is cereal soup? Why or why not?",
		"What secret conspiracy would you like to start?",
		"What's invisible but you wish people could see?",
		"What's the weirdest smell you have ever smelled?",
		"Is a hotdog a sandwich? Why or why not?",
		"What's the best Wi-Fi name you've seen?",
		"What's the most ridiculous/useless fact you know?",
		"What is something that everyone looks stupid doing?",
		"What is the funniest joke you know by heart?",
		"In 40 years, what will people be nostalgic for?",
		"What are the unwritten rules of where you work?",
		"How do you feel about putting pineapple on pizza?",
		"What part of a kid's movie completely scarred you?",
		"What kind of secret society would you like to start?",
		"If animals could talk, which would be the rudest?",
		"Toilet paper, over or under?",
		"What's the best type of cheese?",
		"What's the best inside joke you've been a part of?",
		"In one sentence, how would you sum up the internet?",
		"How many chickens would it take to kill an elephant?",
		"What is the most embarrassing thing you have ever worn?",
		"Which body part do you wish you could detach and why? (keep it family-friendly!)",
		"What used to be considered trashy but now is very classy?",
		"What's the weirdest thing a guest has done at your house?",
		"What mythical creature would improve the world most if it existed?",
		"What inanimate object do you wish you could eliminate from existence?",
		"What is the weirdest thing you have seen in someone else's home?",
		"What would be the worst thing for the government to make illegal?",
		"If peanut butter wasn't called peanut butter, what would it be called?",
		"What movie would be greatly improved if it was made into a musical?",
		"What is the funniest corporate / business screw up you have heard of?",
		"What would be the worst “buy one get one free” sale of all time?",
		"If life were a video game, what would some of the cheat codes be?",
		"What sport would be the funniest to add a mandatory amount of alcohol to?",
		"What would be the coolest animal to scale up to the size of a horse?",
		"What two totally normal things become really weird if you do them back to back?",
		"What set of items could you buy that would make the cashier the most uncomfortable?",
		"What would be the creepiest thing you could say while passing a stranger on the street?",
		"What is something that you just recently realized that you are embarrassed you didn't realize earlier?",
		"Who do you know that really reminds you of a character in a TV show or movie?",
		"What would the world be like if it was filled with male and female copies of you?",
		"What are some things that are okay to occasionally do but definitely not okay to do every day?",
		"If you were arrested with no explanation, what would your friends and family assume you had done?",
		"You're a mad scientist, what scientific experiment would you run if money and ethics weren't an issue?",
		"What are some fun ways to answer everyday questions like “how's it going” or “what do you do”?",
		"If someone asked to be your apprentice and learn all that you know, what would you teach them?",
		"If your five-year-old self suddenly found themselves inhabiting your current body, what would your five-year-old self do first?",
		"What movie completely changes its plot when you change one letter in its title? What's the new movie about?",
		"If the all the States in the USA were represented by food, what food would your state be represented by?",
		"What would some fairy tales be like if they took place in the present and included modern technology and culture?",
		"What is something that is really popular now, but in 5 years everyone will look back on and be embarrassed by?",
		"What ridiculous and untrue, yet slightly plausible, theories can you come up with for the cause of common ailments like headaches or cavities?",
		"If you were transported 400 years into the past with no clothes or anything else, how would you prove that you were from the future?",
		"If you were wrongfully put into an insane asylum, how would you convince them that you're actually sane and not just pretending to be sane?",
		"What fictional character is amazing in their book / show / movie, but would be insufferable if you had to deal with them in mundane everyday situations?",
		"If you were kidnapped and told that if you impress them with your dance moves you would be set free, what dance moves would you bust out?"
	};

	public bool Execute()
	{
		id736.Chat.SetContext(CPH);

		string userName = id736.Chat.GetCurrentUserName();
		if (string.IsNullOrWhiteSpace(userName))
		{
			CPH.LogWarn("[SillyQuestions] could not determine username");
			return false;
		}

		string platform = id736.Chat.GetCurrentPlatform();

		string q = _questions[_random.Next(_questions.Count)];

		string message;
		if (platform == "twitch")
		{
			message = $"TwitchSings @{userName} asks everyone: {q}";
		}
		else
		{
			message = $"@{userName} asks everyone: {q}";
		}

		id736.Chat.SendMessage(message);
		return true;
	}
}
