using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ResourcesHolder : ScriptableObject
{
        private int _stash;
        private int _clip;
        private int _clipsize;

        public void Initialize (int ammo, int clipsize)
        {
            _stash = ammo;
            _clip = clipsize;
            _clipsize = clipsize;
        }
    
        public bool FireBullet ()
        {
            if (_clip > 0)
            {
                _clip -= 1;
                return true;
            }
            else return false;
        }
    
        public void Reload ()
        {
            _stash += _clip;
            _clip = Mathf.Min(_clipsize, _stash);
            _stash -= _clip;
        }
    
        public int GetStash() { return _stash; }
        public int GetClip() { return _clip; }
        public void SetStash(int stashValue) { _stash = stashValue; }
        public void SetClip(int clipValue) { _clip =  clipValue; }
        public void SetClipsize(int clipsizeValue) { _clipsize = clipsizeValue; }
}
