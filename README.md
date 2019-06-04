<img src="https://i.imgur.com/ORpZ51h.png" width="256" height="256">

# PendulumMotion
MotionGraph Editor & API

<br/>

## How to use Editor

#### On editing context
Panning</br>
<kbd>Space</kbd> + <kbd>Mouse Left Drag</kbd> </br>
Zoom</br>
<kbd>Ctrl</kbd> + <kbd>Space</kbd> + <kbd>Mouse Left Drag</kbd> </br>
</br>
Add control point</br>
<kbd>Ctrl</kbd> + <kbd>Mouse Left Click</kbd></br>
Remove control point</br>
<kbd>Alt</kbd> + <kbd>Mouse Left Click</kbd>


<br/><br/>
## How to use API
``` C#
using PendulumMotion;

…

void RegisterMotion() {
  PMotionQuery.LoadFile("C:\motionFile.pmotion", "CustomKey");
}

void UseMotion() {
  for(float time=0f; time<1f; time+=0.01f) {
    float motionTime = PMotionQuery.GetMotionValue("CustomKey", "MotionName", time);
    
    UI.Position = new Vector2(motionTime, 0f);
    
    //It's just example. please don't use it.
    //maybe GameEngine's function is better than this.
    Thread.Sleep(16);
  }
}
```

<br/><br/>
## Preview

![](https://i.imgur.com/7m5raaT.gif)

Design changed

![](https://i.imgur.com/OsbqKGA.png)
