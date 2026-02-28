using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class TutorialSpotlightTarget
{
    public TutorialStep step;
    public Graphic target;
    public string hint;
}

public class TutorialSpotlightController : MonoBehaviour
{
    [SerializeField] private List<TutorialSpotlightTarget> targets = new();
    [SerializeField] private Text hintLabel;
    [SerializeField] private float pulseSpeed = 3f;
    [SerializeField] private float pulseScale = 0.06f;
    [SerializeField] private bool hideWhenTutorialDone = true;

    private readonly Dictionary<Graphic, Color> _baseColors = new();
    private readonly Dictionary<Graphic, Vector3> _baseScales = new();
    private Graphic _active;
    private TutorialStep _lastStep = (TutorialStep)(-1);

    private void Awake()
    {
        RebuildCache();
    }

    private void OnEnable()
    {
        ApplyCurrentStep();
    }

    private void Update()
    {
        ApplyCurrentStep();
        AnimateActive();
    }

    public void Configure(Text newHintLabel, IEnumerable<TutorialSpotlightTarget> newTargets)
    {
        hintLabel = newHintLabel;
        targets = newTargets?.Where(t => t?.target != null).ToList() ?? new List<TutorialSpotlightTarget>();
        RebuildCache();
        _lastStep = (TutorialStep)(-1);
        ApplyCurrentStep();
    }

    private void ApplyCurrentStep()
    {
        var save = Game.Save ?? SaveSystem.LoadOrNew();
        if (save == null) return;

        if (hideWhenTutorialDone && save.tutorialCompleted)
        {
            ClearActive();
            if (hintLabel != null) hintLabel.text = string.Empty;
            return;
        }

        var step = (TutorialStep)Mathf.Clamp(save.tutorialStep, 0, (int)TutorialStep.Complete);
        if (step == _lastStep) return;
        _lastStep = step;

        var entry = targets.FirstOrDefault(t => t != null && t.step == step && t.target != null);
        SetActive(entry?.target);

        if (hintLabel != null)
        {
            hintLabel.text = entry == null ? string.Empty : entry.hint;
        }
    }

    private void SetActive(Graphic next)
    {
        if (_active == next) return;
        ClearActive();
        _active = next;
    }

    private void ClearActive()
    {
        if (_active == null) return;

        if (_baseColors.TryGetValue(_active, out var c))
        {
            _active.color = c;
        }

        if (_baseScales.TryGetValue(_active, out var s))
        {
            _active.rectTransform.localScale = s;
        }

        _active = null;
    }

    private void AnimateActive()
    {
        if (_active == null) return;
        if (!_baseColors.TryGetValue(_active, out var baseColor)) return;
        if (!_baseScales.TryGetValue(_active, out var baseScale)) return;

        float pulse = 0.5f + (Mathf.Sin(Time.unscaledTime * pulseSpeed) * 0.5f);
        _active.color = Color.Lerp(baseColor, Color.white, pulse * 0.35f);
        _active.rectTransform.localScale = baseScale * (1f + (pulse * pulseScale));
    }

    private void RebuildCache()
    {
        _baseColors.Clear();
        _baseScales.Clear();

        foreach (var entry in targets)
        {
            if (entry?.target == null) continue;
            if (!_baseColors.ContainsKey(entry.target)) _baseColors[entry.target] = entry.target.color;
            if (!_baseScales.ContainsKey(entry.target)) _baseScales[entry.target] = entry.target.rectTransform.localScale;
        }
    }
}
