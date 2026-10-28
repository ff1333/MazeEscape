using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FogboundMaze
{
    public sealed class WeaponController : MonoBehaviour
    {
        public const float GunRange = 80f;
        public const float BladeRange = 3.4f;
        public const float GunDamage = 100f;
        public const float BladeDamage = 70f;
        public const int GunMagazineSize = 5;
        public const int StartingReserveAmmunition = 20;
        public const float ReloadDuration = 2.2f;
        public const float ScopeSlowDuration = 3f;
        public const float SniperCooldown = 0.85f;
        public const float BoltCycleDuration = 0.85f;
        private PlayerController owner;
        private Camera viewCamera;
        private Transform viewMount;
        private Transform viewWeapon;
        private Transform muzzleTip;
        private WeaponType type;
        private float nextAttack;
        private float recoil;
        private int ammunition;
        private int reserveAmmunition;
        private bool reloading;
        private bool swinging;
        private bool aiming;
        private bool cyclingBolt;
        private float aimSlowUntil;
        private float boltProgress;
        private float reloadProgress;
        private float reloadReadyUntil;
        private float emptyFeedbackUntil;
        private float nextDryFire;
        private AudioSource audioSource;
        private readonly HashSet<EnemyAgent> struck = new();

        public float ReloadProgress => reloadProgress;
        public WeaponType Type => type;
        public int Ammunition => ammunition;
        public int MagazineSize => GunMagazineSize;
        public int ReserveAmmunition => reserveAmmunition;
        public bool IsReloading => reloading;
        public bool ReloadReady => Time.time < reloadReadyUntil;
        public bool IsSwinging => swinging;
        public bool IsAiming => aiming;
        public bool IsCyclingBolt => cyclingBolt;
        public float BoltProgress => boltProgress;
        public float AimSlowRemaining => aiming ? Mathf.Max(0f, aimSlowUntil - Time.unscaledTime) : 0f;
        public bool EmptyFeedback => Time.time < emptyFeedbackUntil;
        public bool HasLoadout => type != WeaponType.None;
        public float AttackCooldownRemaining => Mathf.Max(0f, nextAttack - Time.time);

        public void Initialize(PlayerController player, Camera camera)
        {
            owner = player;
            viewCamera = camera;
            viewMount = new GameObject("First Person Weapon Mount").transform;
            viewMount.SetParent(viewCamera.transform, false);
            viewMount.localPosition = new Vector3(.32f, -.30f, .65f);
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.spatialBlend = 0f;
            PortfolioSettings.Changed += RefreshVolume;
            RefreshVolume();
        }

        private void LateUpdate()
        {
            viewMount.gameObject.SetActive(viewCamera.GetComponent<CameraRig>().UsesFirstPersonView && !aiming);
            recoil = Mathf.MoveTowards(recoil, 0f, Time.deltaTime * 9f);
            if (viewWeapon != null && !swinging && !reloading && !cyclingBolt)
            {
                viewWeapon.localRotation = Quaternion.Euler(-5f * recoil, 0f, 0f);
                viewWeapon.localPosition = Vector3.back * (.045f * recoil);
            }
        }

        public void PrepareLoadout()
        {
            CancelTransientAction();
            ammunition = GunMagazineSize;
            reserveAmmunition = StartingReserveAmmunition;
            Equip(WeaponType.LongBlade);
            owner.GetComponent<PlayerVisual>()?.Equip(type);
        }

        public void ClearLoadout()
        {
            CancelTransientAction();
            type = WeaponType.None;
            ammunition = reserveAmmunition = 0;
            foreach (Transform child in viewMount) Destroy(child.gameObject);
            viewWeapon = muzzleTip = null;
            owner.GetComponent<PlayerVisual>()?.Equip(type);
        }

        public void Equip(WeaponType weaponType)
        {
            if (weaponType == WeaponType.None)
            {
                ClearLoadout();
                return;
            }
            if (weaponType is not (WeaponType.SniperRifle or WeaponType.LongBlade)) return;
            if (type == weaponType && viewWeapon != null) return;
            CancelTransientAction();
            type = weaponType;
            foreach (Transform child in viewMount) Destroy(child.gameObject);
            viewWeapon = muzzleTip = null;
            owner.GetComponent<PlayerVisual>()?.Equip(type);
            viewMount.localPosition = type == WeaponType.SniperRifle
                ? new Vector3(.32f,-.30f,.65f) : new Vector3(.38f,-.38f,.85f);
            var prefab = Resources.Load<GameObject>(type == WeaponType.SniperRifle ? "Weapons/SniperRifle" : "Weapons/LongBlade");
            viewWeapon = Instantiate(prefab, viewMount).transform;
            muzzleTip = viewWeapon.Find("Muzzle");
            foreach (var child in viewWeapon.GetComponentsInChildren<Transform>(true))
                child.gameObject.layer = LayerMask.NameToLayer("Ignore Raycast");
        }

        public void Tick(bool held, bool pressed, bool reloadPressed, bool aimHeld = false)
        {
            if (type == WeaponType.None) return;
            if (owner.Health.IsDead)
            {
                UpdateAim(false);
                return;
            }
            if (reloading)
            {
                UpdateAim(false);
                return;
            }
            UpdateAim(aimHeld && type == WeaponType.SniperRifle);
            if (cyclingBolt) return;
            if (reloadPressed && type == WeaponType.SniperRifle && ammunition < MagazineSize)
            {
                if (reserveAmmunition > 0) StartCoroutine(Reload());
                return;
            }
            if (Time.time < nextAttack) return;
            if (type == WeaponType.SniperRifle)
            {
                if (!pressed) return;
                if (ammunition <= 0)
                {
                    SignalEmptyMagazine();
                    return;
                }
                nextAttack = Time.time + SniperCooldown;
                Fire();
                StartCoroutine(CycleBolt());
            }
            else
            {
                if (!held && !pressed) return;
                nextAttack = Time.time + .72f;
                owner.GetComponent<PlayerVisual>()?.Attack();
                audioSource.PlayOneShot(ProceduralAudio.Machete);
                NoiseSystem.Emit(transform.position, 7f, NoiseKind.Blade, owner);
                StartCoroutine(Swing());
            }
        }

        private void Fire()
        {
            ammunition--;
            reloadReadyUntil = 0f;
            recoil = 1f;
            viewWeapon.localRotation = Quaternion.Euler(-5f,0f,0f);
            viewWeapon.localPosition = Vector3.back * .045f;
            var aim = new Ray(viewCamera.transform.position, viewCamera.transform.forward);
            var target = aim.GetPoint(GunRange);
            if (Physics.Raycast(aim, out var cameraHit, GunRange, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                target = cameraHit.point;
            var chest = transform.position + Vector3.up * 1.35f;
            var muzzle = chest + transform.right * .32f + transform.forward * .7f;
            var visual = owner.GetComponent<PlayerVisual>();
            if (visual != null && visual.HeldWeapon != null) muzzle = visual.GunMuzzle;
            if (viewCamera.GetComponent<CameraRig>().IsFirstPerson && muzzleTip != null) muzzle = muzzleTip.position;
            var delta = target - muzzle;
            // A camera can see around a corner while the weapon is blocked. Check both segments.
            var blocked = Physics.Linecast(chest, muzzle, out var hit, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            var collided = blocked || (delta.sqrMagnitude > .0001f && Physics.Raycast(muzzle, delta.normalized,
                out hit, Mathf.Min(GunRange, delta.magnitude + .02f), Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore));
            var end = muzzle + delta.normalized * Mathf.Min(GunRange, delta.magnitude);
            if (collided)
            {
                end = hit.point;
                var enemy = hit.collider.GetComponentInParent<EnemyAgent>();
                var hitEnemy = enemy != null && enemy.IsActive;
                if (hitEnemy) ApplyHit(enemy, GunDamage);
                CombatEffects.Impact(hit.point, hit.normal, hitEnemy);
            }
            CombatEffects.Tracer(blocked ? chest : muzzle, end);
            if (!blocked) CombatEffects.Muzzle(muzzle, viewCamera.transform);
            GameDirector.Instance?.Hud?.PulseShot();
            audioSource.PlayOneShot(ProceduralAudio.SniperRifle, .85f);
            NoiseSystem.Emit(transform.position, 42f, NoiseKind.Gunshot, owner);
        }

        private void ApplyHit(EnemyAgent enemy, float damage)
        {
            var health = enemy.GetComponent<Health>();
            var actual = Mathf.Min(damage, health.Current);
            enemy.TakeDamage(damage);
            GameDirector.Instance?.Hud?.ConfirmHit(actual, health.IsDead);
            audioSource.PlayOneShot(ProceduralAudio.Impact, health.IsDead ? .8f : .5f);
        }

        private void ResolveBladeHit()
        {
            var forward = viewCamera.transform.forward;
            forward.y = 0f;
            forward.Normalize();
            var origin = transform.position + Vector3.up * 1.3f;
            CombatEffects.Slash(origin, forward);
            GameDirector.Instance?.Hud?.PulseShot();
            struck.Clear();
            foreach (var collider in Physics.OverlapSphere(origin, BladeRange + .5f,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                var enemy = collider.GetComponentInParent<EnemyAgent>();
                if (enemy == null || !enemy.IsActive || !struck.Add(enemy)) continue;
                var delta = enemy.transform.position - transform.position;
                delta.y = 0f;
                if (delta.sqrMagnitude > BladeRange * BladeRange || Vector3.Dot(forward, delta.normalized) < .5f) continue;
                if (Physics.Linecast(origin, enemy.transform.position + Vector3.up * .8f, out var hit,
                    Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)
                    && hit.collider.GetComponentInParent<EnemyAgent>() == enemy)
                {
                    ApplyHit(enemy, BladeDamage);
                    CombatEffects.Impact(hit.point, hit.normal, true);
                }
            }
            if (Physics.Raycast(origin, forward, out var wall, BladeRange,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)
                && wall.collider.GetComponentInParent<EnemyAgent>() == null)
                CombatEffects.Impact(wall.point, wall.normal);
        }

        private IEnumerator Swing()
        {
            swinging = true;
            var start = Quaternion.Euler(-25f, 25f, -40f);
            var finish = Quaternion.Euler(45f, -35f, 35f);
            for (var t = 0f; t < .12f; t += Time.deltaTime)
            {
                viewWeapon.localRotation = Quaternion.Slerp(Quaternion.identity, start, t / .12f);
                yield return null;
            }
            while (GameDirector.Instance != null && GameDirector.Instance.Phase == GamePhase.Paused)
                yield return null;
            if (owner.Health.IsDead || (GameDirector.Instance != null
                && GameDirector.Instance.Phase is not (GamePhase.Playing or GamePhase.Staging)))
            {
                viewWeapon.localRotation = Quaternion.identity;
                swinging = false;
                yield break;
            }
            ResolveBladeHit();
            for (var t = 0f; t < .18f; t += Time.deltaTime)
            {
                viewWeapon.localRotation = Quaternion.Slerp(start, finish, t / .18f);
                yield return null;
            }
            for (var t = 0f; t < .24f; t += Time.deltaTime)
            {
                viewWeapon.localRotation = Quaternion.Slerp(finish, Quaternion.identity, t / .24f);
                yield return null;
            }
            viewWeapon.localRotation = Quaternion.identity;
            swinging = false;
        }

        private IEnumerator Reload()
        {
            if (reloading || type != WeaponType.SniperRifle || ammunition >= MagazineSize || reserveAmmunition <= 0)
                yield break;
            UpdateAim(false);
            reloading = true;
            emptyFeedbackUntil = 0f;
            audioSource.PlayOneShot(ProceduralAudio.Reload, .7f);
            for (var elapsed = 0f; elapsed < ReloadDuration; elapsed += Time.deltaTime)
            {
                reloadProgress = elapsed / ReloadDuration;
                var dip = Mathf.Sin(reloadProgress * Mathf.PI);
                viewWeapon.localPosition = new Vector3(0f, -.24f * dip, 0f);
                viewWeapon.localRotation = Quaternion.Euler(20f * dip, 0f, -30f * dip);
                yield return null;
            }
            viewWeapon.localPosition = Vector3.zero;
            viewWeapon.localRotation = Quaternion.identity;
            while (GameDirector.Instance != null && GameDirector.Instance.Phase == GamePhase.Paused)
                yield return null;
            if (owner.Health.IsDead || (GameDirector.Instance != null
                && GameDirector.Instance.Phase is not (GamePhase.Playing or GamePhase.Staging)))
            {
                reloadProgress = 0f;
                reloading = false;
                yield break;
            }
            reloadProgress = 1f;
            var transferred = Mathf.Min(MagazineSize - ammunition, reserveAmmunition);
            ammunition += transferred;
            reserveAmmunition -= transferred;
            reloading = false;
            reloadReadyUntil = Time.time + .85f;
            audioSource.PlayOneShot(ProceduralAudio.ReloadComplete, .9f);
        }

        private IEnumerator CycleBolt()
        {
            cyclingBolt = true;
            boltProgress = 0f;
            owner.GetComponent<PlayerVisual>()?.BoltAction(BoltCycleDuration);
            for (var elapsed = 0f; elapsed < BoltCycleDuration; elapsed += Time.deltaTime)
            {
                boltProgress = elapsed / BoltCycleDuration;
                var pull = Mathf.Sin(boltProgress * Mathf.PI);
                viewWeapon.localPosition = new Vector3(.055f * pull, -.025f * pull, -.085f * pull);
                viewWeapon.localRotation = Quaternion.Euler(7f * pull, 0f, -9f * pull);
                if (boltProgress >= .28f && boltProgress - Time.deltaTime / BoltCycleDuration < .28f)
                    audioSource.PlayOneShot(ProceduralAudio.BoltAction, .9f);
                yield return null;
            }
            viewWeapon.localPosition = Vector3.zero;
            viewWeapon.localRotation = Quaternion.identity;
            boltProgress = 1f;
            cyclingBolt = false;
        }

        private void SignalEmptyMagazine()
        {
            emptyFeedbackUntil = Time.time + .65f;
            if (Time.time < nextDryFire) return;
            nextDryFire = Time.time + .3f;
            audioSource.PlayOneShot(ProceduralAudio.DryFire, .65f);
        }

        private void CancelTransientAction()
        {
            StopAllCoroutines();
            reloading = swinging = cyclingBolt = false;
            reloadProgress = boltProgress = recoil = nextAttack = 0f;
            reloadReadyUntil = emptyFeedbackUntil = 0f;
            UpdateAim(false);
        }

        private void UpdateAim(bool value)
        {
            if (value && !aiming) aimSlowUntil = Time.unscaledTime + ScopeSlowDuration;
            aiming = value;
            viewCamera.GetComponent<CameraRig>().SetAiming(aiming);
            GameDirector.Instance?.SetAimSlowMotion(aiming && Time.unscaledTime < aimSlowUntil);
        }

        public void ExitAim() => UpdateAim(false);

        private void OnDestroy()
        {
            PortfolioSettings.Changed -= RefreshVolume;
            if (viewMount != null) Destroy(viewMount.gameObject);
        }
        private void RefreshVolume() { if (audioSource != null) audioSource.volume = PortfolioSettings.Effects; }
    }
}
