NeuronAnimator
==============

01234567890123456789012345678901234567890123456789012345678901234567890123456789

*NeuronAnimator* is a minimum-required subset of the [Perception Neuron][Neuron]
SDK for Unity. This plugin is aiming to provide the following points:

**Retargeting**

The original SDK doesn't support retargeting and models have to be rigged in a
very specific way. With this plugin, you can use any humanoid model with Neuron.

**Simpleness**

There is only one component -- all you have to do is just adding NeuronAnimator
to a game object.

**Robustness**

The original SDK is thread-unsafe and frequently crashes in some environments.
The entire code has been overhauled in this plugin. Should work robustly!

Current status of the project
-----------------------------

This plugin is still under development and not in the final quality. Any feedback
is welcomed :)

License
-------

Copyright © 2015  Beijing Noitom Technologies Ltd. All rights reserved

This plugin is considered as one of "derivative works" of the Perception Neuron
SDK. It means you can use the plugin in the same way as the original SDK. For
further details, please read [the original license document][License].

[Neuron]: https://neuronmocap.com/software/unity-sdk
[License]: https://github.com/keijiro/NeuronAnimator/blob/master/Assets/Neuron/LICENSE.pdf
