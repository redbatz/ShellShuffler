# ShellShuffler
### Shamelessly Shuffles Shells for Shooting Shenanigans

This mod provides the ability for OpFor ammunition loads to be dynamically randomized. Obviously will only work in conjunction with other mods that add additional ammo types.

#### Settings

```
"Settings": {
		"enableLogging": true,
		"shuffleMechs": true,
		"shuffleVehicles": false,
		"shuffleTurrets": true,
		"chanceToShuffle": 0.6,
		"blackListShuffleIn": ["Ammunition_GAUSS_PR"],
    		"blackListShuffleOut": ["Ammunition_GAUSS_PR"],
		"mechDefTagAmmoList": 
			{
			"unit_role_brawler": ["Ammunition_LRM_ER", "Ammunition_LRM_Bee"],
			"Marik": ["Ammunition_LRM_ER"]
			},
		"ammoWeight":
			{
			"Ammunition_LRM_ER": 50
			}
	},
  
  ```
  
`enableLogging` - bool, enables logging
  
`shuffleMechs` - bool, enable/disable ammo shuffling for Mechs
  
`shuffleVehicles` - bool, enable/disable ammo shuffling for Vehicles
   
`shuffleTurrets` - bool, enable/disable ammo shuffling for Turrets

`chanceToShuffle` - float, range 0 - 1.0; probability that ammo shuffling will occur among valid ammo types matching the weapon/ammo `Category` of the original. Thus, LRM ammo will only ever be replaced with LRM ammo, etc. The "pool" of valid ammo types <b>does</b> contain the original ammunition, <b>even if the original ammunition would normally not be allowed per the following settings</b>.

`blackListShuffleIn` - List<string>, list of ammo Id's for ammo that will never be shuffled <b>into</b> a unit. Artemis IV and Nukes should probably go here.

`blackListShuffleOut` - List<string>, list of ammo Id's for ammo that will never be shuffled <b>out of</b> a unit.

`mechDefTagAmmoList` - Dictionary<List<string>>, units with the matching mechdef/vehicledef tag are restricted to shuffling in of ammos in the corresponding list. Where multiple tags are matched on a unit, the result is an intersect of the ammo lists. For example, given the above settings a mech with both the `unit_role_brawler` and `Marik` tags can only shuffle `Ammunition_LRM_ER`

`ammoWeight` - Dictionary<string, int>, ammos in this list have their chances to be chosen during the shuffle increased by the corresponding factor. All ammos not listed have weight of `1`.
