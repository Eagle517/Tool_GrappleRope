datablock AudioProfile(grappleFireSound)
{
	filename    = "./grappleFire.wav";
	description = AudioClosest3d;
	preload = true;
};

datablock ParticleData(ChainTrailParticle)
{
	dragCoefficient = 3.0;
	windCoefficient = 0.0;
	gravityCoefficient = 0.0;
	inheritedVelFactor = 0.0;
	constantAcceleration = 0.0;
	lifetimeMS = 50;
	lifetimeVarianceMS = 0;
	spinSpeed = 10.0;
	spinRandomMin = -50.0;
	spinRandomMax = 50.0;
	useInvAlpha = true;
	animateTexture = false;

	textureName = "base/data/particles/dot";

	colors[0] = "0.2 0.2 0.2 1";
	colors[1] = "0.2 0.2 0.2 1";
	colors[2] = "0.2 0.2 0.2 1";
	sizes[0]  = 0.15;
	sizes[1]  = 0.15;
	sizes[2]  = 0.15;
	times[0]  = 0.0;
	times[1]  = 0.1;
	times[2]  = 1.0;
};

datablock ParticleEmitterData(ChainTrailEmitter)
{
	ejectionPeriodMS = 1;
	periodVarianceMS = 0;
	ejectionVelocity = 0;
	velocityVariance = 0;
	ejectionOffset = 0;
	thetaMin = 0.0;
	thetaMax = 90.0;  
	particles = ChainTrailParticle;
};

datablock ProjectileData(GrappleRopeProjectile)
{
	directDamage = 0;
	radiusDamage = 0;
	damageRadius = 0;

	muzzleVelocity = 200;
	velInheritFactor = 1;

	armingDelay = 0;
	lifetime = 4000;
	fadeDelay = 3500;
	bounceElasticity = 0;
	bounceFriction = 0;
	isBallistic = true;
	gravityMod = 0;

	hasLight = false;
	lightRadius	= 1.0;
	lightColor = "0 0 0";
};

datablock ProjectileData(ChainProjectile)
{
	directDamage = 0;
	radiusDamage = 0;
	damageRadius = 0;
	particleEmitter = ChainTrailEmitter;

	muzzleVelocity = 200;
	velInheritFactor = 1;

	armingDelay = 0;
	lifetime = 4000;
	fadeDelay = 3500;
	bounceElasticity = 0;
	bounceFriction = 0;
	isBallistic = true;
	gravityMod = 0;

	hasLight = false;
 	lightRadius	= 3.0;
	lightColor	= "0 0 0.5";
};

datablock ItemData(GrappleRopeItem)
{
	category = "Tool";
	className = "Tool";

	shapeFile = "base/data/shapes/printGun.dts";
	mass = 1;
	density	= 0.2;
	elasticity = 0.2;
	friction = 0.6;
	emap = true;
	
	uiName = "Grapple Rope";
	iconName = "base/client/ui/itemIcons/printer";
	doColorShift = true;
	colorShiftColor = "1 0.5 0 1";

	image = GrappleRopeImage;
	canDrop	= true;
};

datablock ShapeBaseImageData(GrappleRopeImage)
{
	shapeFile = "base/data/shapes/printGun.dts";
	emap = true;

	mountPoint = 0;
	offset = "0 0 .1";
	eyeOffset = "0.7 1.2 -0.55"; 
	correctMuzzleVector = true;
	className = "WeaponImage";

	item = GrappleRope;
	ammo = " ";
	projectile = GrappleRopeProjectile;
	projectileType = Projectile;

	melee = false;
	doRetraction = false;
	armReady = true;

	doColorShift = true;
	colorShiftColor = "1 0.5 0 1";

	stateName[0]				= "Activate";
	stateTimeoutValue[0]	    = 0.2;
	stateTransitionOnTimeout[0] = "Ready";
	stateScript[0]				= "onRelease";
	stateSound[0]				= weaponSwitchSound;

	stateName[1]                    = "Ready";
	stateTransitionOnTriggerDown[1] = "Fire";
	stateAllowImageChange[1]        = true;

	stateName[2]				= "Fire";
	stateTransitionOnTimeout[2] = "Hold";
	stateTimeoutValue[2]	    = 0.01;
	stateFire[2]				= true;
	stateAllowImageChange[2]    = false;
	stateSequence[2]		    = "Fire";
	stateScript[2]				= "onFire";
	stateWaitForTimeout[2]	    = true;
	stateSound[2]				= grappleFireSound;

	stateName[3]				  = "Hold";
	stateTimeoutValue[3]		  = 0.01;
	stateScript[3]				  = "onHold";
	stateTransitionOnTimeout[3]	  = "Hold";
	stateTransitionOnTriggerUp[3] = "Release";

	statename[4]				= "Release";
	stateAllowImageChange[4]    = false;
	stateTimeoutValue[4]	    = 0.01;
	stateTransitionOnTimeout[4] = "Ready";
	stateScript[4]				= "onRelease";
};

function GrappleRopeImage::onFire(%this, %obj, %slot)
{
	%proj = %this.projectile;
	
	%muz = %obj.getMuzzleVector(%slot);
	%objVel = %obj.getVelocity();
	%vel = vectorAdd(vectorScale(%muz, %proj.muzzleVelocity), vectorScale(%objVel, %proj.velInheritFactor));
	
	%time = getSimTime();
	%p = new (%this.projectileType)()
	{
		datablock = %proj;
		initialVelocity = %vel;
		initialPosition = %obj.getMuzzlePoint(%slot);
		sourceObject = %obj;
		sourceSlot = %slot;
		client = %obj.client;
		
		spawnTime = %time;
	};
	missionCleanup.add(%p);
	
	%obj.lastGrapple = %time;
	%obj.doGrapple = 0;
}

function GrappleRopeProjectile::onCollision(%this, %obj, %col, %fade, %pos, %normal)
{
	%player = %obj.client.player;
	if(%player.lastGrapple != %obj.spawnTime)
		return;
	
	%ppos = %player.getPosition();
	%scanTarg = ContainerRayCast(vectorAdd(%ppos, "0 0 1"), %pos, $TypeMasks::StaticObjectType);
	if(!%scanTarg || %scanTarg == %col)
	{
		%player.ropeLength = vectorLen(vectorSub(%ppos, %pos));
		%player.GrappleRopeCol = %col;
		%player.GrappleRopePos = %pos;
	}
	
	%player.doGrapple = 1;
}

function GrappleRopeImage::onHold(%this, %obj, %slot)
{
	if(%obj.GrappleRopePos !$= "" && %obj.doGrapple)
	{
		%player = %obj.client.player;
		GrappleRope(%player, %player.GrappleRopeCol, %player.GrappleRopeTarget, %player.GrappleRopePos);

		%vec = VectorSub(%player.GrappleRopePos, %obj.getMuzzlePoint(%slot));
		%norm = VectorNormalize(%vec);
		%vel = VectorScale(%norm, 160);

		%p = new (%this.projectileType)()
		{
			dataBlock = ChainProjectile;
			initialVelocity = %vel;
			initialPosition = %obj.getMuzzlePoint(%slot);
			sourceObject = %obj;
			sourceSlot = %slot;
			client = %obj.client;
		};
		missionCleanup.add(%p);
	}
}

function GrappleRopeImage::onRelease(%this, %obj, %slot)
{
	%obj.GrappleRopePos = "";
}

function GrappleRope(%obj, %targetObj, %target, %staticPos, %targetOffset)
{
	if(!isObject(%obj))
		return;
	
	%pos = %staticPos;
	%ppos = %obj.getPosition();
	%dist = getWord(%pos, 0) - getWord(%ppos, 0) SPC getWord(%pos, 1) - getWord(%ppos, 1) SPC getWord(%pos, 2) - getWord(%ppos, 2);
	%vel = %obj.getVelocity();
	%temp = vectorSub(vectorAdd(%ppos, vectorScale(%vel,1)), %pos);
	
	if(vectorLen(%temp) > %obj.ropeLength)
	{
		%temp2 = vectorSub(vectorAdd(vectorScale(%temp, %obj.ropeLength / vectorLen(%temp)), %pos), %ppos);
		%vel = vectorAdd(vectorScale(%temp2, vectorLen(%vel) / vectorLen(%temp2)), vectorScale(vectorSub(%pos, %ppos), 1 / vectorLen(vectorSub(%pos, %ppos))));
	}
	
	%obj.setVelocity(%vel);
}
