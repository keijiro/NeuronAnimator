NeuronAnimator
==============

![gif](https://66.media.tumblr.com/16206b3e0076ad99a6f9850e739ec6fe/tumblr_o96knf6n2S1qio469o1_400.gif)
![gif](https://67.media.tumblr.com/b679c0780f3bf9bd3fe5f7734e04c5ee/tumblr_o986k6HdVU1qio469o1_400.gif)

*NeuronAnimator* is a minimum-required subset of the [Perception Neuron][Neuron]
SDK for Unity. This plugin is aiming to provide the following points:

**Retargetable**

The original SDK doesn't support retargeting, and therefore models have to be
rigged in a very specific way. Within this plugin, you can animate any humanoid
model with Neuron.

**Simpleness**

All you have to do is just adding the NeuronAnimator component to a game
object. Then it starts controlling the model based on input from Neuron mocap
system. That’s that! Simple!

**Robustness**

The original SDK lacks thread-safety and hence crashes periodically in some
environments. To avoid issues like this, the entire code has been overhauled
for this plugin. Should work robustly!

Limitations
-----------

- To retrieve retargeting information properly, the target model has to be
  in a **perfect** T-stance pose. If the default pose of the model looks
  loose (hands are lower than shoulder, feet are widely opened, etc.), it’s
  recommended to straighten up manually.

License
-------

Copyright © 2015  Beijing Noitom Technologies Ltd. All rights reserved

This plugin is considered as one of "derivative works" of the Perception Neuron
SDK. It means you can use the plugin in the same way as the original SDK. For
further details, please read [the original license document][License].

[Neuron]: https://neuronmocap.com/software/unity-sdk
[License]: https://github.com/keijiro/NeuronAnimator/blob/master/Assets/Neuron/LICENSE.pdf
