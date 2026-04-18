=== found_frequency ===
I think I got the right frequency. #speaker:player
-> END

=== incorrect_frequency ===
I don't think this is the right frequency. # speaker:player
-> END

=== right_knob_tuned ===
That sounds good! # speaker:player
-> END

=== incorrect_tuning ===
I don't think this is right. # speaker:player
-> END
 
=== lever_pulled ===
# ui:hide_subtitles
# ui:show_cipher
-> END

===Dragon_Start===
They must have used dragon magic on top of unicorn magic to double-encrypt this message. Now, instead of easily changing the colors with the unicorn horn, you will need to manually shift the color cypher for each word. There should be a clue word showing you what color to shift them all to. # speaker:fern 

I suggest grabbing paper and a writing utensil. #speaker: fern

According to the Wings, unicorn magic in this form shifts the letters in each word by its distance from the correct color in ROYGBIV. So if the word is orange and the correct color is red, each letter is shifted +1 in the alphabet, and should be shifted -1 by you to correct it. #speaker:fern

Once you’ve solved it, type in the decoded message and I'll check if it seems correct. Also, if you click on the message, I'll re-explain the cypher. #speaker:fern
-> END

===Dragon_Hint===
According to the Wings, unicorn magic in this form shifts the letters in each word by its distance from the correct color in ROYGBIV. So if the word is orange and the correct color is red, each letter is shifted +1 in the alphabet, and should be shifted -1 by you to correct it. #speaker:fern
-> END

===Dragon_Wrong===
Unfortunately, I don’t think that’s right, but keep trying! #speaker:fern
-> END

===Dragon_Solve===
Incredible! That surely wasn’t easy. The combination of dragon and unicorn magic is quite formidable! #speaker:fern
What information were they protecting with such a powerful cypher?
+[The enemy is currently mobilizing for an attack.] -> Dragon_Enemy_Attack
+[The dragons are planning an airborne strike.] -> Dragon_Plan
+[The dragons are mobilizing for an airborne strike.] -> Dragon_Attack

===Dragon_Enemy_Attack===
That doesn’t give us much to go off of, but we can at least urgently prepare our troops. What do you think we should do? #speaker:fern
+[Maintain current defensive positions.] -> D1
+[Re-allocate troops for protection against dragon attacks.] -> D2
+[Send troops out to attempt to intercept the attack.] -> D3

===Dragon_Plan===
With this intel, we can completely strategize around their attack! What do you think we should do? #speaker:fern
+[Re-allocate troops for protection against airborne attacks.] -> D4
+[Position troops between here and the mountains to intercept the attack.] -> D5
+[Send troops out to the mountains for a pre-emptive strike.] -> D6

===Dragon_Attack===
Great intel! If we move quickly, we can defend against the attack. #speaker:fern
+[Re-allocate troops for protection against airborne attacks.] -> D7
+[Position troops between here and the mountains to intercept the attack.] -> D8
+[Retaliate with a counter-attack against the dragons.] -> D9

===Siren_Solve===
Incredible! That surely wasn’t easy. The combination of dragon and unicorn magic is quite formidable! #speaker:fern
What information were they protecting with such a powerful cypher?
+[The enemy is currently mobilizing for an attack.] -> Siren_Enemy_Attack
+[The sirens are planning a waterborne strike.] -> Siren_Plan
+[The sirens are mobilizing for a waterborne strike.] -> Siren_Attack

===Siren_Enemy_Attack===
That doesn’t give us much to go off of, but we can at least urgently prepare our troops. What do you think we should do? #speaker:fern
+[Maintain current defensive positions.] -> D1
+[Re-allocate troops for protection against siren attacks.] -> D2
+[Send troops out to attempt to intercept the attack.] -> D3

===Siren_Plan===
With this intel, we can completely strategize around their attack! What do you think we should do? #speaker:fern
+[Re-allocate troops for protection against waterborne attacks.] -> D4
+[Position troops between here and the sea to intercept the attack.] -> D5
+[Send troops out to the sea for a pre-emptive strike.] -> D6

===Siren_Attack===
Great intel! If we move quickly, we can defend against the attack. #speaker:fern
+[Re-allocate troops for protection against waterborne attacks.] -> D7
+[Position troops between here and the sea to intercept the attack.] -> D8
+[Retaliate with a counter-attack against the sirens.] -> D9

===D1===
"Without specific information about who's attacking, we have no reason to adjust our defenses, so holding strong makes sense." #speaker:fern
-> Gazette

===D2===
“Not sure what changed your mind about which enemy is best to prepare our defenses for, but I’ll take that into consideration.” #speaker:fern
-> Gazette

===D3===
“It will be difficult to intercept without a clear direction, but scouting could help us better prepare for when they reach the kingdom.” #speaker:fern
-> Gazette

===D4===
“Now that we know how we are being attacked, adjusting our defensive strategy is practical.” #speaker:fern
-> Gazette

===D5===
“Preparing to intercept the attack could help us distance the fighting from the kingdom and allow us to be more aggressive.” #speaker:fern
-> Gazette

===D6===
“With knowledge of an incoming attack, a pre-emptive strike would be warranted and let us begin neutralizing the enemy forces.” #speaker:fern
-> Gazette

===D7===
“Now that we know how we are being attacked, adjusting our defensive strategy is practical.” #speaker:fern
-> Gazette

===D8===
“Intercepting the attack could help us distance the fighting from the kingdom and allow us to be more aggressive.” #speaker:fern
-> Gazette

===D9===
“A counter-attack would be warranted, and neutralizing enemy forces to prevent future attacks like this one is a good strategy.” #speaker:fern
-> Gazette

=== Gazette ===
# ui:hide_subtitles
A new edition of the Golden Rose Gazette is out. I shoud check it out. # speaker: player

# ui:display_newspaper
-> END




     
     
