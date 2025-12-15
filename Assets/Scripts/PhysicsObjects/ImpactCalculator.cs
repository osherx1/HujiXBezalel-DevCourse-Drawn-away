
namespace Physics.Rock
{


    
    public class ImpactCalculator
    {
        private readonly float _hardImpactThreshold;
        private readonly float _shatterThreshold;

        public ImpactCalculator(float hardThreshold, float shatterThreshold)
        {
            _hardImpactThreshold = hardThreshold;
            _shatterThreshold = shatterThreshold;
        }

        public bool IsHardImpact(float velocity)
        {
            return velocity >= _hardImpactThreshold;
        }

        public bool IsShatterImpact(float velocity)
        {
            return velocity >= _shatterThreshold;
        }
    }
}