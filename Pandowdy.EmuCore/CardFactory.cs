
using Pandowdy.EmuCore.Interfaces;

namespace Pandowdy.EmuCore;

public class CardFactory : ICardFactory
{
    private readonly IEnumerable<ICard> _allCards;

    public CardFactory(IEnumerable<ICard> cards)
    {
        ArgumentNullException.ThrowIfNull(cards);

        // Convert to list to avoid multiple enumeration
        var cardList = cards.ToList();

        // Check for duplicate IDs
        var duplicateIds = cardList
            .GroupBy(card => card.Id)
            .Where(group => group.Count() > 1)
            .Select(group => new
            {
                Id = group.Key,
                Types = string.Join(", ", group.Select(c => c.GetType().Name))
            })
            .ToList();

        if (duplicateIds.Count > 0)
        {
            var errorMessage = string.Join(Environment.NewLine,
                duplicateIds.Select(dup =>
                    $"Card ID {dup.Id} is duplicated by: {dup.Types}"));
            throw new InvalidOperationException(
                $"Duplicate card IDs detected during registration:{Environment.NewLine}{errorMessage}");
        }

        // Check for duplicate names
        var duplicateNames = cardList
            .GroupBy(card => card.Name, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1)
            .Select(group => new
            {
                Name = group.Key,
                Types = string.Join(", ", group.Select(c => c.GetType().Name))
            })
            .ToList();

        if (duplicateNames.Count > 0)
        {
            var errorMessage = string.Join(Environment.NewLine,
                duplicateNames.Select(dup =>
                    $"Card name '{dup.Name}' is duplicated by: {dup.Types}"));
            throw new InvalidOperationException(
                $"Duplicate card names detected during registration:{Environment.NewLine}{errorMessage}");
        }

        _allCards = cardList;
    }

    public ICard? GetCardWithId(int internalId)
    {
        return _allCards.FirstOrDefault(card => card.Id == internalId)?.Clone();
    }

    public ICard? GetCardWithName(string name)
    {
        return _allCards.FirstOrDefault(card => card.Name == name)?.Clone();
    }

    public ICard? GetNullCard()
    {
        return GetCardWithId(0);
    }

    public List<(int, string)> GetAllCardTypes()
    {
        return [.. _allCards
            .Select(card => (card.Id, card.Name))
            .OrderBy(tuple => tuple.Id)];
    }
}
