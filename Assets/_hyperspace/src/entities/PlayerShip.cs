using Photon.Bolt;
using UnityEngine;

namespace Hyperspace.Entities
{
    public class PlayerShip : EntityBehaviour<IPlayerShipState>
    {
        private CraftConfig _initialConfig;
        private CraftState _craftState;
        private ProjectileBase _projectile; 

        public override void Initialized()
        {
            base.Initialized();
            // Hyperspace.Engine.Events.Emit<OnPlayerInitialisedState>(ObjectPool.Get<OnPlayerInitialisedState>().Init(this));
        }

        private void Awake()
        {
            _projectile = new ProjectileBase();
            _craftState = new CraftState
            {
                Position = transform.localPosition
            };
        }

        public override void Attached()
        {
            //Bind the transform to the state transform
            _initialConfig = (CraftConfig)entity.AttachToken;
            state.CraftData.Energy = _initialConfig.Energy;
            
            state.SetTransforms(state.Transform, transform);
            state.OnFire += OnFire;
        }

        public void OnDestroy()
        {
            state.OnFire -= OnFire;
        }
        
        public override void SimulateController()
        {
            ICraftCommandInput input = CraftCommand.Create();
            input = Engine.InputManager.GetInputState(input);
            entity.QueueInput(input);

            if(state.CraftData.Energy < 100)
                state.CraftData.Energy += 1;
        }

        public override void ExecuteCommand(Command command, bool resetState)
        {
            CraftCommand cmd = command as CraftCommand;

            if (resetState)
            {
                SetCraftState(cmd.Result.Position, cmd.Result.Velocity, cmd.Result.Acceleration);
            }
            else
            {
                CraftState craftState = MoveCraft(cmd.Input.Controller, cmd.Input.Thrust);

                cmd.Result.Position = craftState.Position;
                cmd.Result.Velocity = craftState.Velocity;
                cmd.Result.Acceleration = craftState.Acceleration;

                if (cmd.IsFirstExecution)
                {
                    if(cmd.Input.LightFire)
                        FireProjectile(cmd);
                }
            }
        }

        private CraftState MoveCraft(Vector3 inputController, bool inputThrust)
        {
            const float MASS = 1f;
            
            if (inputThrust)
            {
                Vector3 directionToMouse = (inputController - _craftState.Position).normalized;
                _craftState.Acceleration = directionToMouse * _initialConfig.Speed;
                _craftState.Velocity += _craftState.Acceleration  * BoltNetwork.FrameDeltaTime;
            }
            
            Vector3 force = MASS * _craftState.Velocity;
            _craftState.Position += force * BoltNetwork.FrameDeltaTime;
            transform.rotation = Quaternion.LookRotation((Vector3)inputController - _craftState.Position, Vector3.up);
            transform.position = _craftState.Position;
            return _craftState;
        }

        private void SetCraftState(Vector3 resultPosition, Vector3 resultVelocity, Vector3 resultAcceleration)
        {
            _craftState.Position = resultPosition;
            _craftState.Velocity = resultVelocity;
            _craftState.Acceleration = resultAcceleration;
            transform.position = (_craftState.Position - transform.position);
        }
        
        private void FireProjectile(CraftCommand command)
        {
            if (_projectile.fireFrame + _projectile.cooldown <= BoltNetwork.ServerFrame && state.CraftData.Energy - _projectile.cost > 0)
            {
                _projectile.fireFrame = BoltNetwork.ServerFrame;
                state.CraftData.Energy -= _projectile.cost;
                state.Fire();

                if (entity.IsOwner)
                {
                    _projectile.OnOwner(command, entity);
                }
            }
        }
        
        private void OnFire()
        {
            _projectile.OnClient(entity);
        }
        

        /*public override void SimulateController()
        {
            if(!entity.isActiveAndEnabled || !entity.HasControl)
                return;
            
            Vector3? mousePosition = MousePosition();

            if (mousePosition != null)
                transform.rotation = Quaternion.LookRotation((Vector3)mousePosition - transform.position, Vector3.up);

            if (Input.GetKey(KeyCode.W) && mousePosition != null)
                AddVelocity((Vector3)mousePosition);
            
            if(Input.GetKeyDown(KeyCode.Mouse0))
                state.Fire();

            Vector3 force = _mass * _velocity;
            transform.position += force * Time.deltaTime;
        }*/
        
        /*private void AddVelocity(Vector3 mousePosition)
        {
            Vector3 directionToMouse = (mousePosition - this.transform.position).normalized;
            _acceleration = directionToMouse * _initialData.Speed;
            _velocity += _acceleration * BoltNetwork.FrameDeltaTime;
        }*/
        /*private Vector3? MousePosition()
        {
            Vector3? mousePosition = null;
            int layer = (1 << 8);
            Ray ray = Hyperspace.Engine.Camera.MainCamera.ScreenPointToRay(Input.mousePosition);
            if (UnityEngine.Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, layer))
            {
                mousePosition = hit.point;
            }

            return mousePosition;
        }*/
    }
}