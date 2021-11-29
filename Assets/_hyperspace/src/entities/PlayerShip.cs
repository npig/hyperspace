using System.Runtime.InteropServices;
using Photon.Bolt;
using UnityEngine;

namespace Hyperspace.Entities
{
    public class PlayerShip : EntityBehaviour<IPlayerShipState>
    {
        private CraftConfig _initialConfig;
        private ProjectileBase _projectile;
        private Generator _generator; 
        public Vector3 Velocity => _generator.Velocity;
        public Vector3 Position => _generator.Position;
        
        public override void Initialized()
        {
            base.Initialized();
        }

        private void Awake()
        {
            _projectile = new ProjectileBase();
            _generator = new Generator(this, new CraftState(transform.position));
        }

        public override void Attached()
        {
            //Bind the transform to the state transform
            _initialConfig = (CraftConfig)entity.AttachToken;
            state.CraftData.Energy = _initialConfig.Energy;
            
            state.SetTransforms(state.Transform, transform);
            state.OnFire += OnFire;
        }

        private void FixedUpdate()
        {
            _generator.FixedUpdate();
        }

        public void OnDestroy()
        {
            state.OnFire -= OnFire;
        }
        
        public override void SimulateController()
        {
            if (Physics.DetectCollision(Position, Velocity, out RaycastHit hit))
            {
                ICollisionCommandInput collisionInput = CollisionCommand.Create();
                collisionInput.CollisionDetected = true;
                collisionInput.HitNormal = hit.normal;
                collisionInput.Velocity = Velocity;
                collisionInput.Position = Position;
                entity.QueueInput(collisionInput);
            }
            
            ICraftCommandInput input = CraftCommand.Create();
            input = Engine.InputManager.GetInputState(input);
            entity.QueueInput(input);

            if(state.CraftData.Energy < 100)
                state.CraftData.Energy += 1;
        }

        public override void ExecuteCommand(Command command, bool resetState)
        {
            switch (command)
            {
                case CraftCommand cmd when resetState:
                    _generator.SetCraftState(cmd.Result.Position, cmd.Result.Velocity, cmd.Result.Acceleration);
                    break;
                case CraftCommand cmd:
                {
                    CraftState craftState = _generator.ApplyForce(cmd.Input.Controller, cmd.Input.Thrust);
                    cmd.Result.Position = craftState.Position;
                    cmd.Result.Velocity = craftState.Velocity;
                    cmd.Result.Acceleration = craftState.Acceleration;
                    
                    if (cmd.IsFirstExecution)
                    {
                        if(cmd.Input.LightFire)
                            FireProjectile(cmd);
                    }

                    break;
                }
                case CollisionCommand ccmd when resetState:
                    _generator.Velocity = ccmd.Result.CollisionVelocity;
                    _generator.Position = ccmd.Result.CollisionPosition;
                    break;
                case CollisionCommand ccmd:
                {
                    Vector3 collisionVelocity = Physics.ProcessCollision(ccmd.Input.HitNormal, ccmd.Input.Velocity);
                    ccmd.Result.CollisionVelocity = collisionVelocity;
                    ccmd.Result.CollisionPosition = ccmd.Input.Position;
                    _generator.Velocity = collisionVelocity;
                    _generator.Position = ccmd.Input.Position;
                    break;
                }
            }
        }

        /*private void OnCollision(RaycastHit hit)
        {
            float dot = Vector3.Dot(hit.normal, _state.Velocity.normalized);
            Vector3 normalProjection = 2 * dot * hit.normal;
            Vector3 reflection = _state.Velocity.normalized - normalProjection;
            Vector3 result = reflection * (_state.Velocity.magnitude * .8f);
            _state.Velocity = result;
        }*/

        /*private void SetCraftState(Vector3 resultPosition, Vector3 resultVelocity, Vector3 resultAcceleration)
        {
            _state.Position = resultPosition;
            _state.Velocity = resultVelocity;
            _state.Acceleration = resultAcceleration;
            transform.position = (_state.Position - transform.position);
        }*/
        
        /*private void FireProjectile(CraftCommand command)
        {
            if (_projectile.fireFrame + _projectile.cooldown <= BoltNetwork.ServerFrame && state.CraftData.Energy - _projectile.cost > 0)
            {
                _projectile.fireFrame = BoltNetwork.ServerFrame;
                state.CraftData.Energy -= _projectile.cost;
                state.Fire();

                if (entity.IsOwner)
                {
                    _projectile.OnOwner(command, entity, _state);
                }
            }
        }*/
        
        public void FireProjectile(CraftCommand command)
        {
            if (
                _projectile.FireFrame + _projectile.Cooldown <= BoltNetwork.ServerFrame 
                && state.CraftData.Energy - _projectile.Cost > 0
                )
            {
                var fireFrame = BoltNetwork.ServerFrame;
                state.CraftData.Energy -= _projectile.Cost;
                state.Fire();

                if (entity.IsOwner)
                {
                    _projectile.OnOwner(command, entity, _generator.GetCraftState());
                }
            }
        }
        
        private void OnFire()
        {
            _projectile.OnClient(entity, _generator.GetCraftState());
        }
    }

    internal sealed class Generator
    {
        private const float SPEED_MULTIPLIER = 30;
        private EntityBehaviour<IPlayerShipState> _parent;
        private Rigidbody _rigidbody;
        private CraftState _state;

        public BoltEntity Entity => _parent.entity;
        public Vector3 Velocity
        {
            get => _state.Velocity;
            internal set => _state.Velocity = value;
        }
        public Vector3 Acceleration
        {
            get => _state.Acceleration;
            private set => _state.Acceleration = value;
        }
        public Vector3 Position 
        {
            get => _state.Position;
            set => _state.Position = value;
        }

        public Generator(EntityBehaviour<IPlayerShipState> parent, CraftState state)
        {
            _parent = parent;
            _state = state;
            
            _rigidbody = parent.gameObject.GetComponent<Rigidbody>();
            
            if (_rigidbody == null)
                _rigidbody = parent.gameObject.AddComponent<Rigidbody>();

            _rigidbody.isKinematic = true;
            parent.transform.position = _state.Position;
        }

        public CraftState ApplyForce(Vector3 inputAxis, bool inputThrust)
        {
            Vector3 directionToMouse = (inputAxis - Position).normalized;
            /*Quaternion q = Quaternion.LookRotation(directionToMouse, Vector3.up);
            _parent.transform.localRotation = q;*/
            
            if (inputThrust)
            {
                Acceleration = directionToMouse * BoltNetwork.FrameDeltaTime * SPEED_MULTIPLIER;
                Acceleration = Vector3.ClampMagnitude(Acceleration, 2f);
                Velocity += Acceleration * BoltNetwork.FrameDeltaTime;
                Velocity = Vector3.ClampMagnitude(Velocity, 10);
            }

            return _state;
        }

        public void SetCraftState(Vector3 resultPosition, Vector3 resultVelocity, Vector3 resultAcceleration)
        {
            Debug.Log($"{(resultPosition - Position).magnitude}");
            Acceleration += resultAcceleration - Acceleration;
            Velocity += resultVelocity - Velocity;
            Position += resultPosition - Position;
            _parent.transform.position = Position;
            _rigidbody.MovePosition(Position + Velocity);
        }

        public CraftState GetCraftState()
        {
            return _state;
        }

        public void FixedUpdate()
        {
            if(Entity.IsAttached == false || Entity.IsControllerOrOwner == false)
                return; 
            
            _rigidbody.MovePosition(Position + Velocity);

            if (_rigidbody.velocity.magnitude > 10)
            {
                _rigidbody.velocity = Vector3.ClampMagnitude(_rigidbody.velocity, 10);
            }
            
            
            Position = _rigidbody.position;
        }
    }
}