using Riok.Mapperly.Abstractions;
using ReviewService.Application.DTOs;
using ReviewService.Domain.Entities;

namespace ReviewService.Application.Mapping;

[Mapper]
public partial class ReviewMapper
{
	public partial ReviewDto ReviewToReviewDto(Review review);
	public partial IEnumerable<ReviewDto> ReviewsToReviewDtos(IEnumerable<Review> reviews);

	[MapperIgnoreTarget(nameof(Review.Id))]
	[MapperIgnoreTarget(nameof(Review.UserId))]
	[MapperIgnoreTarget(nameof(Review.CreatedAt))]
	[MapperIgnoreTarget(nameof(Review.UpdatedAt))]
	public partial Review CreateReviewDtoToReview(CreateReviewDto dto);

	[MapperIgnoreSource(nameof(UpdateReviewDto.Id))]
	[MapperIgnoreTarget(nameof(Review.Id))]
	[MapperIgnoreTarget(nameof(Review.UserId))]
	[MapperIgnoreTarget(nameof(Review.LocationId))]
	[MapperIgnoreTarget(nameof(Review.CreatedAt))]
	[MapperIgnoreTarget(nameof(Review.UpdatedAt))]
	public partial void UpdateReviewFromDto(UpdateReviewDto dto, Review review);
}
