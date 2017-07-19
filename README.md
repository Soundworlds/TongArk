# TongArk
Software reveals the interactive duet “human / software agents” in which the latter perceives the part performed by the human agent, and makes an ambient sound in its own musical part using the genetic algorithm. 
The human agent controls the software agent’s performance by means of some change in emotions shown on the face. The software agent estimates the affective state of the human performer using face recognition to generate accompaniment. Every single change of any affective state is reflected in updating the timbre and reverberation characteristics used by a computer system of sound elements as well as in transforming the sounding in all of the software agent’s part. 
The system design combines Affective Computing, GAs, and machine listening. 

You need a web-camera for human agent face recogntion and a mic for human agent sound capture.




The structure of sound patterns file (temporally for today) should be as followed:



[duration]      // key word, don't change


40              // not important, now never used


[directories]   // key word, don't change


14           	// important: name of a directory inside Sound Elements folder where sound files have been stored


14            	// important: name of a directory inside Sound Elements folder where sound files have been stored


14              // important: name of a directory inside Sound Elements folder where sound files have been stored


[blank space]   // just blank space


Later I'll change it to xml file structure or something.
