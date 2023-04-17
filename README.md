# ShellShuffler

**Versions 1.1.0.0 and higher requires modtek v3 or higher**

### Shamelessly Shuffles Shells for Shooting Shenanigans

This mod provides the ability for OpFor ammunition loads to be dynamically randomized. Obviously will only work in conjunction with other mods that add additional ammo types.

#### Settings

```
	"Settings": {
		"enableLogging": true,
		"shuffleMechs": true,
		"shuffleVehicles": false,
		"shuffleTurrets": true,
		"tagSetsUnion": false,
		"chanceToShuffle": 0.6,
		"unShuffledBins": 1,
		"blackListShuffleIn": [
			"Ammunition_GAUSS_PR"
		],
		"blackListShuffleOut": [
			"Ammunition_GAUSS_PR"
		],
		"mechDefTagAmmoList": {
			"unit_role_brawler": [
				"Ammunition_LRM_ER",
				"Ammunition_LRM_Bee"
			],
			"unit_role_sniper": [
				"Ammunition_LRM_ER",
				"Ammunition_LRM_DF"
			]
		},
		"factionAmmoList": {
			"Marik": [
				"Ammunition_LRM_PR",
				"Ammunition_LRM_Bee"
			],
			"AuriganPirates": [
				"Ammunition_AC5_Piercing",
				"Ammunition_AC5_AoE",
				"Ammunition_LRM_PR",
				"Ammunition_LRM_Bee"
			]
		},
		"ammoWeight": {
			"Ammunition_LRM_ER": 50
		}
	}
  
  ```
  
`enableLogging` - bool, enables logging
  
`shuffleMechs` - bool, enable/disable ammo shuffling for Mechs
  
`shuffleVehicles` - bool, enable/disable ammo shuffling for Vehicles
   
`shuffleTurrets` - bool, enable/disable ammo shuffling for Turrets

`tagSetsUnion` - bool, determines behavior when unit contains multiple tags in `mechDefTagAmmoList`

`chanceToShuffle` - float, range 0 - 1.0; probability that ammo shuffling will occur among valid ammo types matching the weapon/ammo `Category` of the original. Thus, LRM ammo will only ever be replaced with LRM ammo, etc. The "pool" of valid ammo types <b>does</b> contain the original ammunition, <b>even if the original ammunition would normally not be allowed per the following settings</b>.

`unShuffledBins` - int, number of same-ammo-category bins that will <b>not</b> be shuffled on the unit. Default value is 1: in this case, a unit with a single bin of LRM ammo would not shuffle. A unit with 2 bins of LRM ammo would shuffle only 1 of the bins, while the other would remain original to the unitDef. A unit with 3 bins of LRM ammo would shuffle 2 of the 3 bins, etc. Set to 0 to allow shuffling of all bins.

`blackListShuffleIn` - List<string>, list of ammo Id's for ammo that will never be shuffled <b>into</b> a unit. Artemis IV and Nukes should probably go here.

`blackListShuffleOut` - List<string>, list of ammo Id's for ammo that will never be shuffled <b>out of</b> a unit.

`mechDefTagAmmoList` - Dictionary<List<string>>, units with the matching mechdef/vehicledef tag are restricted to shuffling in of ammos in the corresponding list. Where multiple tags are matched on a unit, the result depends on the value of `tagSetsUnion`. If `tagSetsUnion = false`, the result is an intersect of the ammo lists. For example, given the above settings a mech with both the `unit_role_brawler` and `unit_role_sniper` tags can only shuffle `Ammunition_LRM_ER`. If `tagSetsUnion = true`, the result is a union of the ammo lists. For example, given the above settings a mech with both the `unit_role_brawler` and `unit_role_sniper` tags can shuffle any of `Ammunition_LRM_ER`, `Ammunition_LRM_Bee`, or `Ammunition_LRM_DF`.
	
`factionAmmoList` - Dictionary<List<string>>, restricts the pool of available ammos based on factionID. Does <b>not</b> override results of `mechDefTagAmmoList`

`ammoWeight` - Dictionary<string, int>, ammos in this list have their chances to be chosen during the shuffle increased by the corresponding factor. All ammos not listed have weight of `1`.
